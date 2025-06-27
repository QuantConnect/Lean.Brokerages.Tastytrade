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
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

namespace QuantConnect.Brokerages.Tastytrade;

/// <summary>
/// Represents the Tastytrade <see cref="IDataQueueHandler"/> implementation.
/// </summary>
public partial class TastytradeBrokerage : IDataQueueHandler
{
    /// <summary>
    /// Aggregates ticks and bars based on given subscriptions.
    /// </summary>
    private IDataAggregator _aggregator;

    /// <summary>
    /// Sets the job we're subscribing for
    /// </summary>
    /// <param name="job">Job we're subscribing for</param>
    public void SetJob(LiveNodePacket job)
    {
        if (!job.BrokerageData.TryGetValue("tastytrade-api-url", out var baseUrl) || string.IsNullOrEmpty(baseUrl))
        {
            baseUrl = "https://api.tastyworks.com";
        }

        if (!job.BrokerageData.TryGetValue("tastytrade-websocket-url", out var baseWSUrl) || string.IsNullOrEmpty(baseWSUrl))
        {
            baseWSUrl = "wss://streamer.tastyworks.com";
        }

        Initialize(
            baseUrl: baseUrl,
            baseWSUrl: baseWSUrl,
            username: job.BrokerageData.TryGetValue("tastytrade-username", out var username) ? username : string.Empty,
            password: job.BrokerageData.TryGetValue("tastytrade-password", out var password) ? password : string.Empty,
            accountNumber: job.BrokerageData.TryGetValue("tastytrade-account-number", out var accountNumber) ? accountNumber : string.Empty,
            refreshToken: job.BrokerageData.TryGetValue("tastytrade-refresh-token", out var refreshToken) ? refreshToken : string.Empty,
            orderProvider: null,
            securityProvider: null,
            algorithm: null);

        if (!IsConnected)
        {
            Connect();
        }
    }

    /// <summary>
    /// Subscribe to the specified configuration
    /// </summary>
    /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
    /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
    /// <returns>The new enumerator for this subscription request</returns>
    public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
    {
        if (!CanSubscribe(dataConfig.Symbol))
        {
            return null;
        }

        var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
        _levelOneServiceManager.Subscribe(dataConfig);

        return enumerator;
    }

    /// <summary>
    /// Removes the specified configuration
    /// </summary>
    /// <param name="dataConfig">Subscription config to be removed</param>
    public void Unsubscribe(SubscriptionDataConfig dataConfig)
    {
        _levelOneServiceManager.Unsubscribe(dataConfig);
        _aggregator.Remove(dataConfig);
    }

    /// <summary>
    /// Adds the specified symbols to the subscription
    /// </summary>
    /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
    private bool Subscribe(IEnumerable<Symbol> symbols)
    {
        _clientWrapperByWebSocketType[WebSocketType.MarketData].Send(new FeedSubscription(symbols, _symbolMapper).ToJson());
        return true;
    }

    /// <summary>
    /// Removes the specified symbols to the subscription
    /// </summary>
    /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
    private bool Unsubscribe(IEnumerable<Symbol> symbols)
    {
        _clientWrapperByWebSocketType[WebSocketType.MarketData].Send(new FeedUnSubscription(symbols, _symbolMapper).ToJson());
        return true;
    }

    private void OnTradeReceived(TradeContent trade, Symbol leanSymbol, DateTime tradeDateTime)
    {
        if (trade.Price <= 0 && trade.Size <= 0)
        {
            return;
        }

        _levelOneServiceManager.HandleLastTrade(leanSymbol, tradeDateTime, trade.Size, trade.Price);
    }

    private void OnQuoteReceived(QuoteContent quote, Symbol leanSymbol, DateTime quoteDateTime)
    {
        _levelOneServiceManager.HandleQuote(leanSymbol, quoteDateTime, quote.BidPrice, quote.BidSize, quote.AskPrice, quote.AskSize);
    }

    private void OnSummaryReceived(SummaryContent summary, Symbol leanSymbol, DateTime summaryDateTime)
    {
        _levelOneServiceManager.HandleOpenInterest(leanSymbol, summaryDateTime, summary.OpenInterest);
    }

    private void OnCandleReceived(CandleContent candle)
    {
        if (_historyStreams.TryGetValue(candle.Symbol, out var candleFeedService))
        {
            switch (candle.EventFlag)
            {
                case EventFlag.SnapshotBegin:
                    break;
                case EventFlag.SnapshotSnip:
                case EventFlag.SnapshotEnd:
                    candleFeedService.SnapshotCompletedEvent.Set();
                    return;
            }

            candleFeedService.Add(candle.DateTime, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume);
        }
    }
}
