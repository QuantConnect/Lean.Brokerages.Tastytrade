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
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;
using StreamDataResponse = QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData.Data;

namespace QuantConnect.Brokerages.Tastytrade.Converters;

public class FeedDataConverter : JsonConverter<StreamDataResponse>
{
    /// <summary>
    /// Number of fields expected for each trade entry in the market data array.
    /// Format: [symbol, price, size, time]
    /// </summary>
    private const int TradeFieldCount = 4;

    /// <summary>
    /// Number of fields expected for each quote entry in the market data array.
    /// Format: [symbol, bidPrice, bidSize, askPrice, askSize]
    /// </summary>
    private const int QuoteFieldCount = 5;

    /// <summary>
    /// Number of fields expected for each summary entry in the market data array.
    /// Format: [symbol, openInterest]
    /// </summary>
    private const int SummaryFieldCount = 2;

    /// <summary>
    /// Gets a value indicating whether this <see cref="JsonConverter"/> can write JSON.
    /// </summary>
    /// <value><c>true</c> if this <see cref="JsonConverter"/> can write JSON; otherwise, <c>false</c>.</value>
    public override bool CanWrite => false;

    /// <summary>
    /// Gets a value indicating whether this <see cref="JsonConverter"/> can read JSON.
    /// </summary>
    /// <value><c>true</c> if this <see cref="JsonConverter"/> can read JSON; otherwise, <c>false</c>.</value>
    public override bool CanRead => true;

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <param name="existingValue">The existing property value of the JSON that is being converted.</param>
    /// <param name="serializer">The calling serializer.</param>
    /// <returns>The object value.</returns>
    public override StreamDataResponse ReadJson(JsonReader reader, Type objectType, StreamDataResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jToken = JArray.Load(reader);

        var eventType = jToken[0].ToString() switch
        {
            "Trade" => MarketDataEvent.Trade,
            "Quote" => MarketDataEvent.Quote,
            "Summary" => MarketDataEvent.Summary,
            _ => throw new NotSupportedException($"{nameof(FeedDataConverter)}.{nameof(ReadJson)}.")
        };

        switch (eventType)
        {
            case MarketDataEvent.Trade:
                return new StreamDataResponse(eventType, ConvertArrayToTradeContent(jToken[1] as JArray));
            case MarketDataEvent.Quote:
                return new StreamDataResponse(eventType, ConvertArrayToQuoteContent(jToken[1] as JArray));
            case MarketDataEvent.Summary:
                return new StreamDataResponse(eventType, ConvertArrayToSummaryContent(jToken[1] as JArray));
            default:
                throw new NotImplementedException($"{nameof(FeedDataConverter)}.{nameof(ReadJson)}.");
        }
    }

    /// <summary>
    /// Parses a JSON array representing market summaries and converts it into an array of <see cref="SummaryContent"/> instances.
    /// </summary>
    /// <param name="jArray">
    /// A <see cref="JArray"/> where each summary entry consists of a fixed set of fields:
    /// [symbol, openInterest].
    /// </param>
    /// <returns>
    /// An array of <see cref="SummaryContent"/> objects populated from the provided JSON array.
    /// </returns>
    private static SummaryContent[] ConvertArrayToSummaryContent(JArray jArray)
    {
        var summaries = new SummaryContent[jArray.Count / SummaryFieldCount];
        for (int i = 0, j = 0; i < summaries.Length; i++, j += SummaryFieldCount)
        {
            summaries[i] = new SummaryContent(
                symbol: jArray[j].ToString(),
                openInterest: decimal.TryParse(jArray[j + 1].ToString(), out var price) ? price : 0m);
        }
        return summaries;
    }

    /// <summary>
    /// Parses a JSON array representing trades and converts it into an array of <see cref="TradeContent"/> instances.
    /// </summary>
    /// <param name="jArray">
    /// A <see cref="JArray"/> where each trade entry consists of a fixed set of fields
    /// (symbol, price, size, and Unix timestamp in milliseconds).
    /// </param>
    /// <returns>
    /// An array of <see cref="TradeContent"/> objects populated from the provided JSON array.
    /// </returns>
    private static TradeContent[] ConvertArrayToTradeContent(JArray jArray)
    {
        var trades = new TradeContent[jArray.Count / TradeFieldCount];
        for (int i = 0, j = 0; i < trades.Length; i++, j += TradeFieldCount)
        {
            trades[i] = new TradeContent(
                symbol: jArray[j].ToString(),
                price: decimal.TryParse(jArray[j + 1].ToString(), out var price) ? price : 0m,
                size: decimal.TryParse(jArray[j + 2].ToString(), out var size) ? size : 0m,
                tradeDateTime: Time.UnixMillisecondTimeStampToDateTime(jArray[j + 3].Value<decimal>())
            );
        }
        return trades;
    }

    /// <summary>
    /// Parses a JSON array representing quote data and converts it into an array of <see cref="QuoteContent"/> instances.
    /// </summary>
    /// <param name="jArray">
    /// A <see cref="JArray"/> where each quote entry consists of a fixed set of fields
    /// (symbol, bid price, ask price, bid size, and ask size).
    /// </param>
    /// <returns>
    /// An array of <see cref="QuoteContent"/> objects populated from the provided JSON array.
    /// </returns>
    private static QuoteContent[] ConvertArrayToQuoteContent(JArray jArray)
    {
        var quotes = new QuoteContent[jArray.Count / QuoteFieldCount];
        for (int i = 0, j = 0; i < quotes.Length; i++, j += QuoteFieldCount)
        {
            quotes[i] = new QuoteContent(
                symbol: jArray[j].ToString(),
                bidPrice: decimal.TryParse(jArray[j + 1].ToString(), out var bidPrice) ? bidPrice : 0m,
                askPrice: decimal.TryParse(jArray[j + 2].ToString(), out var askPrice) ? askPrice : 0m,
                bidSize: decimal.TryParse(jArray[j + 3].ToString(), out var bidSize) ? bidSize : 0m,
                askSize: decimal.TryParse(jArray[j + 4].ToString(), out var askSize) ? askSize : 0m
                );
        }
        return quotes;
    }

    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
    /// <param name="value">The value.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override void WriteJson(JsonWriter writer, StreamDataResponse value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
