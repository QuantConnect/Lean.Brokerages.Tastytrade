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
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Converters;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

/// <summary>
/// Represents a response containing feed data with a specific event type and channel.
/// </summary>
public sealed class FeedData : BaseMarketDataResponse
{
    /// <summary>
    /// Gets the data content of the feed, deserialized using a custom converter.
    /// </summary>
    [JsonConverter(typeof(FeedDataConverter))]
    public Data Data { get; set; }
}

/// <summary>
/// Represents the core data content of a feed, which includes the event type and associated content.
/// </summary>
public class Data
{
    /// <summary>
    /// Gets the type of market data event (e.g., Trade, Quote).
    /// </summary>
    public MarketDataEvent EventType { get; }

    /// <summary>
    /// Gets the collection of content items related to the event.
    /// </summary>
    public IReadOnlyCollection<BaseContent> Content { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Data"/> class.
    /// </summary>
    /// <param name="eventType">The type of market data event.</param>
    /// <param name="content">The collection of content associated with the event.</param>
    [JsonConstructor]
    public Data(MarketDataEvent eventType, IReadOnlyCollection<BaseContent> content)
    {
        EventType = eventType;
        Content = content;
    }

}

/// <summary>
/// Represents a base class for market data content items, such as trades or quotes.
/// </summary>
public class BaseContent
{
    /// <summary>
    /// Gets the symbol (ticker) associated with the content.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseContent"/> class.
    /// </summary>
    /// <param name="symbol">The symbol associated with the content.</param>
    [JsonConstructor]
    public BaseContent(string symbol)
    {
        Symbol = symbol;
    }
}

/// <summary>
/// Represents a trade content item with price, size, and timestamp information.
/// </summary>
public sealed class TradeContent : BaseContent
{
    /// <summary>
    /// Gets the trade price.
    /// </summary>
    public decimal Price { get; }

    /// <summary>
    /// Gets the trade size (volume).
    /// </summary>
    public decimal Size { get; }

    /// <summary>
    /// Gets the date and time of the trade.
    /// </summary>
    public DateTime TradeDateTime { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TradeContent"/> class.
    /// </summary>
    /// <param name="symbol">The traded symbol.</param>
    /// <param name="price">The price of the trade.</param>
    /// <param name="size">The size of the trade.</param>
    /// <param name="tradeDateTime">The timestamp of the trade.</param>
    public TradeContent(string symbol, decimal price, decimal size, DateTime tradeDateTime)
        : base(symbol)
    {
        Price = price;
        Size = size;
        TradeDateTime = tradeDateTime;
    }
}

/// <summary>
/// Represents quote content with bid/ask prices and sizes.
/// </summary>
public sealed class QuoteContent : BaseContent
{
    /// <summary>
    /// Gets the current bid price.
    /// </summary>
    public decimal BidPrice { get; }

    /// <summary>
    /// Gets the current ask price.
    /// </summary>
    public decimal AskPrice { get; }

    /// <summary>
    /// Gets the current bid size.
    /// </summary>
    public decimal BidSize { get; }

    /// <summary>
    /// Gets the current ask size.
    /// </summary>
    public decimal AskSize { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuoteContent"/> class.
    /// </summary>
    /// <param name="symbol">The quoted symbol.</param>
    /// <param name="bidPrice">The bid price.</param>
    /// <param name="askPrice">The ask price.</param>
    /// <param name="bidSize">The bid size.</param>
    /// <param name="askSize">The ask size.</param>
    public QuoteContent(string symbol, decimal bidPrice, decimal askPrice, decimal bidSize, decimal askSize)
        : base(symbol)
    {
        BidPrice = bidPrice;
        AskPrice = askPrice;
        BidSize = bidSize;
        AskSize = askSize;
    }
}
