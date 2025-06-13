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

using System.Linq;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;

/// <summary>
/// Provides a base class for feed subscriptions, mapping security types to default market data events.
/// </summary>
public abstract class BaseFeedSubscription
{
    /// <summary>
    /// Maps each <see cref="SecurityType"/> to the array of <see cref="MarketDataEvent"/>s
    /// that should be subscribed to by default.
    /// </summary>
    /// <remarks>
    /// For security types that support <see cref="TickType.OpenInterest"/>, the subscription
    /// includes <see cref="MarketDataEvent.Summary"/> (which provides OpenInterest updates)
    /// along with <see cref="MarketDataEvent.Trade"/> and <see cref="MarketDataEvent.Quote"/>.
    /// For other security types, only <see cref="MarketDataEvent.Trade"/> and <see cref="MarketDataEvent.Quote"/>
    /// are included.
    /// </remarks>
    private static readonly Dictionary<SecurityType, MarketDataEvent[]> SecurityTypeToMarketDataEvents = Data.SubscriptionManager.DefaultDataTypes()
        .ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Contains(TickType.OpenInterest)
                ? new[] { MarketDataEvent.Trade, MarketDataEvent.Quote, MarketDataEvent.Summary }
                : new[] { MarketDataEvent.Trade, MarketDataEvent.Quote }
        );

    /// <summary>
    /// Gets the event type associated with the subscription.
    /// </summary>
    public EventType Type => EventType.FeedSubscription;

    /// <summary>
    /// Gets the channel number used for the subscription.
    /// </summary>
    public int Channel => 1;

    public abstract IReadOnlyList<SymbolType> SymbolTypes { get; }

    /// <summary>
    /// Creates a read-only list of <see cref="SymbolType"/> instances from the specified symbols using the default market data event mapping.
    /// </summary>
    /// <param name="symbols">The symbols for which to create the symbol-type pairs.</param>
    /// <param name="symbolMapper">The brokerage symbol mapper used to resolve brokerage-specific symbol representations.</param>
    /// <returns>A read-only list of symbol-type pairs.</returns>
    protected static IReadOnlyList<SymbolType> CreateSymbolTypes(IEnumerable<Symbol> symbols, TastytradeBrokerageSymbolMapper symbolMapper)
    {
        var symbolTypes = new List<SymbolType>();

        foreach (var symbol in symbols)
        {
            foreach (var type in SecurityTypeToMarketDataEvents[symbol.SecurityType])
            {
                symbolTypes.Add(new SymbolType(symbolMapper.GetBrokerageSymbols(symbol).brokerageStreamMarketDataSymbol, type));
            }
        }

        return symbolTypes;
    }
}
