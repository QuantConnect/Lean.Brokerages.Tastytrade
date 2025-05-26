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
using QuantConnect.Brokerages.Tastytrade.Serialization;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

public abstract class BaseFeedSubscription
{
    private static readonly string[] _defaultSubscriptionTypes = ["Trade", "Quote"];

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

/// <summary>
/// Represents a feed subscription for specified symbols and types (e.g., Trade, Quote).
/// </summary>
public sealed class FeedSubscription : BaseFeedSubscription
{
    /// <summary>
    /// Gets the list of symbol-type pairs to subscribe to.
    /// </summary>
    [JsonProperty("add", Order = 3)]
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

/// <summary>
/// Represents a feed unsubscription for specified symbols and types.
/// </summary>
public sealed class FeedUnSubscription : BaseFeedSubscription
{
    /// <summary>
    /// Gets the list of symbol-type pairs to unsubscribe from.
    /// </summary>
    [JsonProperty("remove", Order = 3)]
    public override IReadOnlyList<SymbolType> SymbolTypes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedUnSubscription"/> class.
    /// </summary>
    /// <param name="tickers">The ticker symbols to unsubscribe from.</param>
    public FeedUnSubscription(IEnumerable<string> tickers)
    {
        SymbolTypes = CreateSymbolTypes(tickers);
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

/// <summary>
/// Represents a symbol and the type of data associated with it (e.g., Trade or Quote).
/// </summary>
public readonly struct SymbolType
{
    /// <summary>
    /// Gets the symbol name.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the type of data for the symbol (e.g., "Trade", "Quote").
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolType"/> struct.
    /// </summary>
    /// <param name="symbol">The symbol name.</param>
    /// <param name="type">The type of data.</param>
    public SymbolType(string symbol, string type) => (Symbol, Type) = (symbol, type);
}
