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
using QuantConnect.Brokerages.Tastytrade.Serialization;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

/// <summary>
/// Represents a feed unsubscription for specified symbols and types.
/// </summary>
public sealed class FeedUnSubscription : BaseFeedSubscription
{
    /// <summary>
    /// Gets the list of symbol-type pairs to unsubscribe from.
    /// </summary>
    [JsonProperty("remove")]
    public override IReadOnlyList<SymbolType> SymbolTypes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedUnSubscription"/> class with the specified symbols.
    /// </summary>
    /// <param name="symbols">The symbols to unsubscribe from.</param>
    /// <param name="symbolMapper">The brokerage symbol mapper used to resolve brokerage-specific symbol representations.</param>
    public FeedUnSubscription(IEnumerable<Symbol> symbols, TastytradeBrokerageSymbolMapper symbolMapper)
    {
        SymbolTypes = CreateSymbolTypes(symbols, symbolMapper);
    }

    /// <summary>
    /// Converts the current object to its JSON representation.
    /// </summary>
    /// <returns>A JSON string representation of the unsubscription.</returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, JsonSettings.CamelCase);
    }
}
