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
using QuantConnect.Securities.Option;
using QuantConnect.Brokerages.Tastytrade.Models;
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
    /// <summary>
    /// Provides thread-safe management of historical candle feed data requests,
    /// ensuring serialized processing per symbol while allowing parallel operations across different symbols.
    /// </summary>
    private readonly Dictionary<Symbol, CandleFeedContext> _historyLockStreams = [];

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
    /// Indicates whether a warning about an invalid time range has already been logged.
    /// </summary>
    private volatile bool _invalidTimeRangeWarningLogged;

    /// <summary>
    /// Indicates whether the expired option contract warning has already been fired.
    /// Prevents repeated warnings for expired option subscriptions.
    /// </summary>
    private bool _expiredOptionContractWarningFired;

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
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UnsupportedSecurityType", $"SecurityType '{request.Symbol.SecurityType}' for symbol '{request.Symbol}' is not supported."));
            }
            return null;
        }

        if (request.TickType == TickType.Quote)
        {
            if (!_invalidTickTypeWarningFired)
            {
                _invalidTickTypeWarningFired = true;
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UnsupportedTickType", $"TickType '{request.TickType}' is not supported. Only '{nameof(TickType.Trade)}' and '{nameof(TickType.OpenInterest)}' are allowed."));
            }
            return null;
        }

        if (request.Symbol.SecurityType.IsOption() && OptionSymbol.IsOptionContractExpired(request.Symbol, DateTime.UtcNow))
        {
            if (!_expiredOptionContractWarningFired)
            {
                _expiredOptionContractWarningFired = true;
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ExpiredOptionContract", $"Historical data for the expired option contract '{request.Symbol}' is not available."));
            }
            return null;
        }

        if (request.StartTimeUtc >= request.EndTimeUtc)
        {
            if (!_invalidTimeRangeWarningLogged)
            {
                _invalidTimeRangeWarningLogged = true;
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidDateRange", "The history request start date must precede the end date, no history returned"));
            }

            return null;
        }

        var context = default(CandleFeedContext);
        while (true)
        {
            lock (_historyLockStreams)
            {
                if (!_historyLockStreams.TryGetValue(request.Symbol, out context))
                {
                    context = new CandleFeedContext(request);
                    _historyLockStreams[request.Symbol] = context;
                    break;
                }
            }

            context.FinishResetEvent.WaitOne();
        }

        SendCandleFeedRequest(request.Symbol, request.Resolution, request.StartTimeUtc, (s, r, t) => new CandleFeedSubscription(s, r, t));

        try
        {
            if (!context.CandleFeedService.SnapshotCompletedEvent.WaitOne(TimeSpan.FromSeconds(20)))
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"{nameof(TastytradeBrokerage)}.{nameof(GetHistory)}: Timeout waiting for snapshot data." +
                    $"Request details - Symbol: {request.Symbol.Value} ({request.Symbol.SecurityType}), Resolution: {request.Resolution}, StartTimeUtc: {request.StartTimeUtc:u}, EndTimeLocal: {request.EndTimeLocal:u}."));
                return null;
            }

            return FilterHistory(context.CandleFeedService.Candles, request, request.StartTimeLocal, request.EndTimeLocal);
        }
        finally
        {
            SendCandleFeedRequest(request.Symbol, request.Resolution, request.StartTimeUtc, (s, r, t) => new CandleFeedUnsubscription(s, r, t));

            lock (_historyLockStreams)
            {
                _historyLockStreams.Remove(request.Symbol);
                context.FinishResetEvent.Set();
                context.Dispose();
            }
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
    /// Attempts to retrieve the <see cref="CandleFeedService"/> associated with the specified symbol.
    /// </summary>
    /// <param name="symbol">The symbol for which to retrieve the candle feed service.</param>
    /// <param name="candleFeedService">Outputs the found <see cref="CandleFeedService"/> if it exists; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the candle feed service was found; otherwise, <c>false</c>.</returns>
    private bool TryGetCandleFeedService(Symbol symbol, out CandleFeedService candleFeedService)
    {
        candleFeedService = default;
        lock (_historyLockStreams)
        {
            if (_historyLockStreams.TryGetValue(symbol, out var candleFeedContext))
            {
                candleFeedService = candleFeedContext.CandleFeedService;
                return true;
            }
        }
        Log.Error($"{nameof(TastytradeBrokerage)}.{nameof(TryGetCandleFeedService)}: CandleFeedService not found for symbol: {symbol}");
        return false;
    }

    /// <summary>
    /// Constructs and sends a candle feed subscription or unsubscription message to the market data websocket.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the candle feed message to send, which must inherit from <see cref="BaseFeedSubscription"/>.
    /// Typically <c>CandleFeedSubscription</c> or <c>CandleFeedUnsubscription</c>.
    /// </typeparam>
    /// <param name="symbol">
    /// The base LEAN <see cref="Symbol"/> to subscribe or unsubscribe (e.g., <c>AAPL</c>, <c>SPY</c>).
    /// </param>
    /// <param name="resolution">
    /// The resolution of the candle data to request (e.g., <see cref="Resolution.Minute"/>, <see cref="Resolution.Hour"/>).
    /// </param>
    /// <param name="startDateTimeUtc">
    /// The UTC start time for the subscription or unsubscription.
    /// </param>
    /// <param name="createFeedMessage">
    /// A factory delegate that creates the feed message using the brokerage-formatted symbol,
    /// resolution, and start time. The formatted symbol typically includes a postfix to indicate resolution
    /// (e.g., <c>AAPL{=1d}</c>).
    /// </param>
    private void SendCandleFeedRequest<T>(
        Symbol symbol,
        Resolution resolution,
        DateTime startDateTimeUtc,
        Func<string, Resolution, DateTime, T> createFeedMessage)
        where T : BaseFeedSubscription
    {
        var brokerageSymbol = _symbolMapper.GetBrokerageSymbols(symbol).brokerageStreamMarketDataSymbol;

        var feedMessage = createFeedMessage(brokerageSymbol, resolution, startDateTimeUtc);

        if (Log.DebuggingEnabled)
        {
            Log.Debug($"{nameof(TastytradeBrokerage)}.{nameof(SendCandleFeedRequest)}.{typeof(T).Name}.WS.Message: {feedMessage.ToJson()}");
        }

        _clientWrapperByWebSocketType[WebSocketType.MarketData].Send(feedMessage.ToJson());
    }
}
