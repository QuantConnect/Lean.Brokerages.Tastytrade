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
using QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

/// <summary>
/// Represents a feed unsubscription request for candle (OHLC) data for a specific symbol and resolution.
/// </summary>
public class CandleFeedUnSubscription : BaseFeedSubscription
{
    /// <summary>
    /// Gets the collection of candle subscription requests to be removed.
    /// </summary>
    [JsonProperty("remove")]
    public IReadOnlyCollection<CandleSubscriptionRequest> Candles { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CandleFeedUnSubscription"/> class for a specific symbol, resolution, and start time.
    /// </summary>
    /// <param name="symbol">The base symbol to unsubscribe from (e.g., "AAPL").</param>
    /// <param name="resolution">The resolution of the candle data (e.g., Minute, Hour, Daily).</param>
    /// <param name="startDateTime">The starting time for the unsubscription. Will be converted to Unix timestamp in seconds.</param>
    public CandleFeedUnSubscription(string symbol, Resolution resolution, DateTime startDateTime)
    {
        Candles = [new(symbol, resolution, startDateTime)];
    }
}
