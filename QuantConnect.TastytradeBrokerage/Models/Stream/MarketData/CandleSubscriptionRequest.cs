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
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

/// <summary>
/// Represents a request to subscribe to candle (OHLC) market data for a specific symbol, starting from a given time.
/// </summary>
public readonly struct CandleSubscriptionRequest
{
    /// <summary>
    /// Gets the symbol for which candle data is requested.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the type of market data event. Always returns <see cref="MarketDataEvent.Candle"/>.
    /// </summary>
    public MarketDataEvent Type => MarketDataEvent.Candle;

    /// <summary>
    /// Gets the starting time for the candle subscription, represented as a Unix timestamp in milliseconds.
    /// </summary>
    public long FromTime { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CandleSubscriptionRequest"/> struct with the specified symbol, resolution, and start time.
    /// </summary>
    /// <param name="symbol">The base symbol to subscribe to (e.g., "AAPL"). The resolution-specific postfix will be appended automatically.</param>
    /// <param name="resolution">The resolution of the candle data (e.g., Minute, Hour, Daily).</param>
    /// <param name="fromTime">The starting time for the subscription. The value is converted to a Unix timestamp in seconds.</param>
    public CandleSubscriptionRequest(string symbol, Resolution resolution, DateTime fromTime)
    {
        Symbol = resolution.GetSymbolWithPeriodPostfix(symbol);
        FromTime = (long)Time.DateTimeToUnixTimeStampMilliseconds(fromTime);
    }
}
