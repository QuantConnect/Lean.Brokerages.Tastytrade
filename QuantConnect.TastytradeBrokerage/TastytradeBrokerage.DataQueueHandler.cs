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
using System.Linq;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
    /// Sets the job we're subscribing for
    /// </summary>
    /// <param name="job">Job we're subscribing for</param>
    public void SetJob(LiveNodePacket job)
    {
        throw new NotImplementedException();
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
        _subscriptionManager.Subscribe(dataConfig);

        return enumerator;
    }

    /// <summary>
    /// Removes the specified configuration
    /// </summary>
    /// <param name="dataConfig">Subscription config to be removed</param>
    public void Unsubscribe(SubscriptionDataConfig dataConfig)
    {
        _subscriptionManager.Unsubscribe(dataConfig);
        _aggregator.Remove(dataConfig);
    }

    /// <summary>
    /// Adds the specified symbols to the subscription
    /// </summary>
    /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
    protected override bool Subscribe(IEnumerable<Symbol> symbols)
    {
        // TODO: use SymbolMapper
        var brokerageSymbols = symbols.Select(x => x.Value);

        MarketDataUpdatesWebSocket.Send(new FeedSubscription(brokerageSymbols).ToJson());

        return true;
    }

    /// <summary>
    /// Removes the specified symbols to the subscription
    /// </summary>
    /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
    private bool Unsubscribe(IEnumerable<Symbol> symbols)
    {
        // TODO: use SymbolMapper
        var brokerageSymbols = symbols.Select(x => x.Value);

        MarketDataUpdatesWebSocket.Send(new FeedUnSubscription(brokerageSymbols).ToJson());

        return true;
    }

    protected override void OnTradeReceived(TradeContent trade)
    {
        var leanSymbol = trade.Symbol; // TODO: Get LeanSymbol

        if (!_exchangeTimeZoneByLeanSymbol.TryGetValue(leanSymbol, out var exchangeTimeZone))
        {
            return;
        }

        var tick = new Tick(DateTime.UtcNow.ConvertFromUtc(exchangeTimeZone), leanSymbol, string.Empty, string.Empty, trade.Size, trade.Price);

        lock (_synchronizationContext)
        {
            _aggregator.Update(tick);
        }
    }

    protected override void OnQuoteReceived(QuoteContent quote)
    {
        throw new NotImplementedException();
    }
}
