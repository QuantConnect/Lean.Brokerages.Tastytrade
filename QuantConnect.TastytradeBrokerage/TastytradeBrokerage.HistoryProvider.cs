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
using System.Linq;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

namespace QuantConnect.Brokerages.Tastytrade;

/// <summary>
/// Represents the Tastytrade History Provider implementation.
/// </summary>
public partial class TastytradeBrokerage
{
    private readonly ConcurrentDictionary<string, BlockingCollection<BaseData>> _historyStreams = new();

    /// <summary>
    /// Gets the history for the requested symbols
    /// <see cref="IBrokerage.GetHistory(HistoryRequest)"/>
    /// </summary>
    /// <param name="request">The historical data request</param>
    /// <returns>An enumerable of bars covering the span specified in the request</returns>
    public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
    {
        var dataQueue = new BlockingCollection<BaseData>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(100));

        var feedSymbol = SendCandleFeedRequest(true, request.Symbol, request.Resolution, request.StartTimeUtc);

        _historyStreams[feedSymbol] = dataQueue;

        cts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));

        using (dataQueue)
        {
            try
            {
                var dataQueueEnumerable = dataQueue.GetConsumingEnumerable(cts.Token);

                if (!dataQueueEnumerable.Any())
                {
                    return null;
                }

                return dataQueueEnumerable;
            }
            finally
            {
                feedSymbol = SendCandleFeedRequest(false, request.Symbol, request.Resolution, request.StartTimeUtc);
                _historyStreams.TryRemove(feedSymbol, out _);
            }
        }
    }

    /// <summary>
    /// Sends a subscription or unsubscription request for candle data and returns the formatted brokerage symbol.
    /// </summary>
    /// <param name="isSubscribeCandleMessage">True to subscribe; false to unsubscribe.</param>
    /// <param name="symbol">The LEAN symbol to subscribe/unsubscribe.</param>
    /// <param name="resolution">The resolution of the candle data.</param>
    /// <param name="startDateTimeUtc">The start time for the subscription in UTC.</param>
    /// <returns>The formatted brokerage symbol with period postfix.</returns>
    private string SendCandleFeedRequest(bool isSubscribeCandleMessage, Symbol symbol, Resolution resolution, DateTime startDateTimeUtc)
    {
        var brokerageStreamMarketDataSymbol = _symbolMapper.GetBrokerageSymbols(symbol).brokerageStreamMarketDataSymbol;

        BaseFeedSubscription feedMessage = isSubscribeCandleMessage
            ? new CandleFeedSubscription(brokerageStreamMarketDataSymbol, resolution, startDateTimeUtc)
            : new CandleFeedUnSubscription(brokerageStreamMarketDataSymbol, resolution, startDateTimeUtc);

        var brokerageSymbol = feedMessage switch
        {
            CandleFeedSubscription subscription => subscription.Candles.First().Symbol,
            CandleFeedUnSubscription unsubscription => unsubscription.Candles.First().Symbol,
            _ => throw new InvalidOperationException("Unsupported feed message type.")
        };

        _clientWrapperByWebSocketType[WebSocketType.MarketData].Send(feedMessage.ToJson());

        return brokerageSymbol;
    }
}
