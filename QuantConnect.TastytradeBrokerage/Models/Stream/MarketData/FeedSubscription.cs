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
/// Represents a feed subscription for specified symbols and types (e.g., Trade, Quote).
/// </summary>
public sealed class FeedSubscription : BaseFeedSubscription
{
    /// <summary>
    /// Gets the list of symbol-type pairs to subscribe to.
    /// </summary>
    [JsonProperty("add")]
    public override IReadOnlyList<SymbolType> SymbolTypes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSubscription"/> class.
    /// </summary>
    /// <param name="tickers">The ticker symbols to subscribe to.</param>
    public FeedSubscription(IEnumerable<string> tickers)
    {
        SymbolTypes = CreateSymbolTypes(tickers);
    }

    /// <summary>
    /// Converts the current object to its JSON representation.
    /// </summary>
    /// <returns>A JSON string representation of the subscription.</returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, JsonSettings.CamelCase);
    }
}