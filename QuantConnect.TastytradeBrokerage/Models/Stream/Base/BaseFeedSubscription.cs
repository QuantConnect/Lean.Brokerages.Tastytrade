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

using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;

public abstract class BaseFeedSubscription
{
    private static readonly MarketDataEvent[] _defaultSubscriptionTypes = [MarketDataEvent.Trade, MarketDataEvent.Quote];

    /// <summary>
    /// Gets the event type associated with the subscription.
    /// </summary>
    [JsonProperty(Order = 1)]
    public EventType Type => EventType.FeedSubscription;

    /// <summary>
    /// Gets the channel number used for the subscription.
    /// </summary>
    [JsonProperty(Order = 2)]
    public int Channel => 1;

    public abstract IReadOnlyList<SymbolType> SymbolTypes { get; }

    /// <summary>
    /// Initializes a new list of SymbolType from tickers and default types.
    /// </summary>
    /// <param name="tickers">The ticker symbols.</param>
    /// <returns>A read-only list of symbol-type pairs.</returns>
    protected static IReadOnlyList<SymbolType> CreateSymbolTypes(IEnumerable<string> tickers)
    {
        var symbols = new List<SymbolType>();

        foreach (var ticker in tickers)
        {
            foreach (var type in _defaultSubscriptionTypes)
            {
                symbols.Add(new SymbolType(ticker, type));
            }
        }

        return symbols;
    }
}
