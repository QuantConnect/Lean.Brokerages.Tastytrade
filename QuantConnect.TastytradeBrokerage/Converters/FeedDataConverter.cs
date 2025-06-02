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
using System.Collections.Generic;
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
            _ => throw new NotSupportedException($"{nameof(FeedDataConverter)}.{nameof(ReadJson)}.")
        };

        switch (eventType)
        {
            case MarketDataEvent.Trade:
                return new StreamDataResponse(eventType, ConvertArrayToTradeContent(jToken[1] as JArray));
            case MarketDataEvent.Quote:
                return new StreamDataResponse(eventType, ConvertArrayToQuoteContent(jToken[1] as JArray));
            default:
                throw new NotImplementedException($"{nameof(FeedDataConverter)}.{nameof(ReadJson)}.");
        }
    }


    /// <summary>
    /// Converts a JArray of trade entries to a collection of <see cref="TradeContent"/>.
    /// </summary>
    /// <param name="jArray">The JSON array representing trade data.</param>
    /// <returns>A read-only collection of <see cref="TradeContent"/>.</returns>
    private static IReadOnlyCollection<TradeContent> ConvertArrayToTradeContent(JArray jArray)
    {
        var trades = new List<TradeContent>();
        for (int i = 0; i < jArray.Count; i += TradeFieldCount)
        {
            trades.Add(new TradeContent(
                symbol: jArray[i].ToString(),
                price: decimal.TryParse(jArray[i + 1].ToString(), out var price) ? price : 0m,
                size: decimal.TryParse(jArray[i + 2].ToString(), out var size) ? size : 0m,
                tradeDateTime: Time.UnixMillisecondTimeStampToDateTime(jArray[i + 3].Value<decimal>())
                ));
        }
        return trades;
    }

    /// <summary>
    /// Converts a JArray of quote entries to a collection of <see cref="QuoteContent"/>.
    /// </summary>
    /// <param name="jArray">The JSON array representing quote data.</param>
    /// <returns>A read-only collection of <see cref="QuoteContent"/>.</returns>
    private static IReadOnlyCollection<QuoteContent> ConvertArrayToQuoteContent(JArray jArray)
    {
        var quotes = new List<QuoteContent>();
        for (int i = 0; i < jArray.Count; i += QuoteFieldCount)
        {
            quotes.Add(new QuoteContent(
                symbol: jArray[i].ToString(),
                bidPrice: decimal.TryParse(jArray[i + 1].ToString(), out var bidPrice) ? bidPrice : 0m,
                askPrice: decimal.TryParse(jArray[i + 2].ToString(), out var askPrice) ? askPrice : 0m,
                bidSize: decimal.TryParse(jArray[i + 3].ToString(), out var bidSize) ? bidSize : 0m,
                askSize: decimal.TryParse(jArray[i + 4].ToString(), out var askSize) ? askSize : 0m
                ));
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
