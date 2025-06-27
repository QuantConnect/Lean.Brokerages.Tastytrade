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
using System.Linq;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

/// <summary>
/// Represents a candle feed message containing a collection of candle subscription requests.
/// </summary>
public interface ICandleFeedMessage
{
    /// <summary>
    /// Gets the collection of candle subscription requests included in the message.
    /// </summary>
    IReadOnlyCollection<CandleSubscriptionRequest> Candles { get; }

    /// <summary>
    /// Returns the symbol with resolution postfix from the first candle subscription request in the collection.
    /// </summary>
    /// <returns>The symbol string with resolution postfix (e.g., "AAPL{=1d}").</returns>
    /// <exception cref="InvalidOperationException">Thrown if the collection of candles is empty.</exception>
    public string GetFirstSymbolWithResolution() => Candles.First().Symbol;
}
