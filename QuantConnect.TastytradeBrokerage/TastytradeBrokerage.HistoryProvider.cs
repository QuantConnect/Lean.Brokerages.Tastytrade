/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Brokerages.Tastytrade.Services;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

namespace QuantConnect.Brokerages.Tastytrade;

/// <summary>
/// Represents the Tastytrade History Provider implementation.
/// </summary>
public partial class TastytradeBrokerage
{
    private readonly ConcurrentDictionary<string, CandleFeedService> _historyStreams = new();

    /// <summary>
    /// Indicates whether the warning for invalid <see cref="SecurityType"/> has been fired.
    /// </summary>
    private volatile bool _invalidSecurityTypeWarningFired;

    /// <summary>
    /// Indicates whether a warning has been triggered for an invalid TickType request.
    /// </summary>
    /// <remarks>
    /// This flag is set to true when an invalid TickType request.
    /// </remarks>
    private volatile bool _invalidTickTypeWarningFired;

    /// <summary>
    /// Gets the history for the requested symbols
    /// <see cref="IBrokerage.GetHistory(HistoryRequest)"/>
    /// </summary>
    /// <param name="request">The historical data request</param>
    /// <returns>An enumerable of bars covering the span specified in the request</returns>
    public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
    {
        if (!CanSubscribe(request.Symbol))
        {
            if (!_invalidSecurityTypeWarningFired)
            {
                _invalidSecurityTypeWarningFired = true;
                Log.Trace($"{nameof(TastytradeBrokerage)}.{nameof(GetHistory)}: Unsupported SecurityType '{request.Symbol.SecurityType}' for symbol '{request.Symbol}'");
            }
            return null;
        }

        if (request.TickType == TickType.Quote)
        {
            if (!_invalidTickTypeWarningFired)
            {
                _invalidTickTypeWarningFired = true;
                Log.Trace($"{nameof(TastytradeBrokerage)}.{nameof(GetHistory)}: Request error: only 'Trade' TickType is supported. You requested '{request.TickType}'.");
            }
            return null;
        }

        var candleFeedService = new CandleFeedService(request.Symbol, request.Resolution);

        var feedSymbol = SendCandleFeedRequest(request.Symbol, request.Resolution, request.StartTimeUtc, (s, r, t) => new CandleFeedSubscription(s, r, t));

        _historyStreams[feedSymbol] = candleFeedService;

        try
        {
            if (!candleFeedService.SnapshotCompletedEvent.WaitOne(TimeSpan.FromSeconds(100)))
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"{nameof(TastytradeBrokerage)}.{nameof(GetHistory)}: Timeout waiting for snapshot data." +
                    $"Request details - Symbol: {request.Symbol.Value} ({request.Symbol.SecurityType}), Resolution: {request.Resolution}, StartTimeUtc: {request.StartTimeUtc:u}, EndTimeLocal: {request.EndTimeLocal:u}."));
                return null;
            }

            return FilterHistory(candleFeedService.Candles, request, request.StartTimeLocal, request.EndTimeLocal);
        }
        finally
        {
            feedSymbol = SendCandleFeedRequest(request.Symbol, request.Resolution, request.StartTimeUtc, (s, r, t) => new CandleFeedUnsubscription(s, r, t));
            candleFeedService?.Dispose();
            _historyStreams.TryRemove(feedSymbol, out _);
        }
    }

    private IEnumerable<BaseData> FilterHistory(IEnumerable<BaseData> history, HistoryRequest request, DateTime startTimeLocal, DateTime endTimeLocal)
    {
        // cleaning the data before returning it back to user
        foreach (var bar in history)
        {
            if (bar.Time >= startTimeLocal && bar.EndTime <= endTimeLocal)
            {
                if (request.ExchangeHours.IsOpen(bar.Time, bar.EndTime, request.IncludeExtendedMarketHours))
                {
                    yield return bar;
                }
            }
        }
    }

    /// <summary>
    /// Builds and sends a candle feed subscription or unsubscription message, then returns the formatted symbol with resolution postfix.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the candle feed message to send, which must implement both <see cref="BaseFeedSubscription"/> and <see cref="ICandleFeedMessage"/>.
    /// </typeparam>
    /// <param name="symbol">The base LEAN symbol to subscribe or unsubscribe (e.g., "AAPL").</param>
    /// <param name="resolution">The resolution of the candle data (e.g., Minute, Hour, Daily).</param>
    /// <param name="startDateTimeUtc">The UTC start time for the subscription or unsubscription.</param>
    /// <param name="createFeedMessage">A factory delegate that constructs the candle feed message using the brokerage-formatted symbol, resolution, and start time.</param>
    /// <returns>The formatted brokerage symbol with resolution postfix (e.g., "AAPL{=1d}").</returns>
    private string SendCandleFeedRequest<T>(
        Symbol symbol,
        Resolution resolution,
        DateTime startDateTimeUtc,
        Func<string, Resolution, DateTime, T> createFeedMessage)
        where T : BaseFeedSubscription, ICandleFeedMessage
    {
        var brokerageSymbol = _symbolMapper.GetBrokerageSymbols(symbol).brokerageStreamMarketDataSymbol;

        var feedMessage = createFeedMessage(brokerageSymbol, resolution, startDateTimeUtc);

        if (Log.DebuggingEnabled)
        {
            Log.Debug($"{nameof(TastytradeBrokerage)}.{nameof(SendCandleFeedRequest)}.{typeof(T).Name}.WS.Message: {feedMessage.ToJson()}");
        }

        _clientWrapperByWebSocketType[WebSocketType.MarketData].Send(feedMessage.ToJson());

        return feedMessage.GetFirstSymbolWithResolution();
    }
}
