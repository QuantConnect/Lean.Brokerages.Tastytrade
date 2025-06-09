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
using NodaTime;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Packets;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Brokerages.Tastytrade.Services;
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
    /// Use like synchronization context for threads
    /// </summary>
    private readonly Lock _synchronizationContext = new();

    /// <summary>
    /// A thread-safe dictionary that maps a <see cref="Symbol"/> to a <see cref="DateTimeZone"/>.
    /// </summary>
    /// <remarks>
    /// This dictionary is used to store the time zone information for each symbol in a concurrent environment,
    /// ensuring thread safety when accessing or modifying the time zone data.
    /// </remarks>
    private readonly ConcurrentDictionary<Symbol, DateTimeZone> _exchangeTimeZoneByLeanSymbol = new();

    /// <summary>
    /// A thread-safe dictionary that stores the order books by brokerage symbols.
    /// </summary>
    private readonly ConcurrentDictionary<string, LevelOneService> _levelOneServices = new();

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
            accountNumber: null,
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
        SubscriptionManager.Subscribe(dataConfig);

        return enumerator;
    }

    /// <summary>
    /// Removes the specified configuration
    /// </summary>
    /// <param name="dataConfig">Subscription config to be removed</param>
    public void Unsubscribe(SubscriptionDataConfig dataConfig)
    {
        SubscriptionManager.Unsubscribe(dataConfig);
        _aggregator.Remove(dataConfig);
    }

    /// <summary>
    /// Adds the specified symbols to the subscription
    /// </summary>
    /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
    private bool Subscribe(IEnumerable<Symbol> symbols)
    {
        var brokerageStreamSymbols = new List<string>();
        foreach (var symbol in symbols)
        {
            if (!_exchangeTimeZoneByLeanSymbol.TryGetValue(symbol, out _))
            {
                _exchangeTimeZoneByLeanSymbol[symbol] = symbol.GetSymbolExchangeTimeZone();
            }

            var brokerageStreamSymbol = _symbolMapper.GetBrokerageSymbols(symbol).brokerageStreamMarketDataSymbol;

            if (!_levelOneServices.TryGetValue(brokerageStreamSymbol, out _))
            {
                _levelOneServices[brokerageStreamSymbol] = new(symbol);
                _levelOneServices[brokerageStreamSymbol].BestBidAskUpdated += OnBestBidAskUpdated;
            }

            brokerageStreamSymbols.Add(brokerageStreamSymbol);
        }

        MarketDataUpdatesWebSocket.Send(new FeedSubscription(brokerageStreamSymbols).ToJson());

        return true;
    }

    /// <summary>
    /// Removes the specified symbols to the subscription
    /// </summary>
    /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
    private bool Unsubscribe(IEnumerable<Symbol> symbols)
    {
        var brokerageStreamSymbols = new List<string>();
        foreach (var symbol in symbols)
        {
            _exchangeTimeZoneByLeanSymbol.Remove(symbol, out _);

            var brokerageStreamSymbol = _symbolMapper.GetBrokerageSymbols(symbol).brokerageStreamMarketDataSymbol;

            if (_levelOneServices.TryRemove(brokerageStreamSymbol, out var orderBook))
            {
                orderBook.BestBidAskUpdated -= OnBestBidAskUpdated;
            }

            brokerageStreamSymbols.Add(brokerageStreamSymbol);
        }

        MarketDataUpdatesWebSocket.Send(new FeedUnSubscription(brokerageStreamSymbols).ToJson());

        return true;
    }

    private void OnTradeReceived(TradeContent trade)
    {
        if (trade.Price <= 0 && trade.Size <= 0)
        {
            return;
        }

        if (_levelOneServices.TryGetValue(trade.Symbol, out var orderBook))
        {
            if (!_exchangeTimeZoneByLeanSymbol.TryGetValue(orderBook.Symbol, out var exchangeTimeZone))
            {
                return;
            }

            var tick = new Tick(DateTime.UtcNow.ConvertFromUtc(exchangeTimeZone), orderBook.Symbol, string.Empty, string.Empty, trade.Size, trade.Price);

            lock (_synchronizationContext)
            {
                _aggregator.Update(tick);
            }
        }
        else
        {
            Log.Error($"{nameof(TastytradeBrokerage)}.{nameof(OnTradeReceived)}: Symbol {trade.Symbol} not found in order books. This could indicate an unexpected symbol or a missing initialization step.");
        }
    }

    private void OnQuoteReceived(QuoteContent quote)
    {
        if (_levelOneServices.TryGetValue(quote.Symbol, out var levelOneService))
        {
            UpdateLevelOneRow(quote.AskPrice, quote.AskSize, levelOneService.BestAskPrice, levelOneService.BestAskSize, levelOneService.UpdateAskRow);
            UpdateLevelOneRow(quote.BidPrice, quote.BidSize, levelOneService.BestBidPrice, levelOneService.BestBidSize, levelOneService.UpdateBidRow);
        }
        else
        {
            Log.Error($"{nameof(TastytradeBrokerage)}.{nameof(OnQuoteReceived)}: Symbol {quote.Symbol} not found in order books. This could indicate an unexpected symbol or a missing initialization step.");
        }
    }

    private void OnSummaryReceived(SummaryContent summary)
    {
        if (_levelOneServices.TryGetValue(summary.Symbol, out var levelOneService))
        {
            if (!_exchangeTimeZoneByLeanSymbol.TryGetValue(levelOneService.Symbol, out var exchangeTimeZone))
            {
                return;
            }

            var tick = new Tick(DateTime.UtcNow.ConvertFromUtc(exchangeTimeZone), levelOneService.Symbol, summary.OpenInterest);

            lock (_synchronizationContext)
            {
                _aggregator.Update(tick);
            }
        }
        else
        {
            Log.Error($"{nameof(TastytradeBrokerage)}.{nameof(OnSummaryReceived)}: Symbol {summary.Symbol} not found in order books. This could indicate an unexpected symbol or a missing initialization step.");
        }
    }

    /// <summary>
    /// Updates a level one row with the given price and size, considering best available values if needed.
    /// </summary>
    /// <param name="price">The price to update, if available.</param>
    /// <param name="size">The size associated with the price, if available.</param>
    /// <param name="bestPrice">The best known price to use as a fallback if <paramref name="price"/> is unavailable.</param>
    /// <param name="bestSize">The best known size to use as a fallback if <paramref name="size"/> is unavailable.</param>
    /// <param name="updateRowAction">The action to execute to update the row.</param>
    internal static void UpdateLevelOneRow(decimal? price, decimal? size, decimal bestPrice, decimal bestSize, Action<decimal, decimal> updateRowAction)
    {
        if (size.HasValue && size.Value != 0)
        {
            if (price.HasValue && price.Value != 0)
            {
                updateRowAction(price.Value, size.Value);
            }
            else if (bestPrice != 0)
            {
                updateRowAction(bestPrice, size.Value);
            }
        }
        else if (price.HasValue && price.Value != 0)
        {
            updateRowAction(price.Value, bestSize);
        }
    }

    /// <summary>
    /// Handles updates to the best bid and ask prices and updates the aggregator with a new quote tick.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="bestBidAskUpdatedEvent">The event arguments containing best bid and ask details.</param>
    private void OnBestBidAskUpdated(object sender, BestBidAskUpdatedEventArgs bestBidAskUpdatedEvent)
    {
        if (!_exchangeTimeZoneByLeanSymbol.TryGetValue(bestBidAskUpdatedEvent.Symbol, out var exchangeTimeZone))
        {
            return;
        }

        var tick = new Tick
        {
            AskPrice = bestBidAskUpdatedEvent.BestAskPrice,
            BidPrice = bestBidAskUpdatedEvent.BestBidPrice,
            Time = DateTime.UtcNow.ConvertFromUtc(exchangeTimeZone),
            Symbol = bestBidAskUpdatedEvent.Symbol,
            TickType = TickType.Quote,
            AskSize = bestBidAskUpdatedEvent.BestAskSize,
            BidSize = bestBidAskUpdatedEvent.BestBidSize
        };
        tick.SetValue();

        lock (_synchronizationContext)
        {
            _aggregator.Update(tick);
        }
    }
}
