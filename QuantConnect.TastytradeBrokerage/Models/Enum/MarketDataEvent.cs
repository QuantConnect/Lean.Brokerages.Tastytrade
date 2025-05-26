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
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Tastytrade.Models.Enum;

/// <summary>
/// Represents the type of market data event contained in a feed message.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum MarketDataEvent
{
    /// <summary>
    /// A quote update event, containing bid and ask price/size data.
    /// </summary>
    Quote = 1,

    /// <summary>
    /// A trade event, representing an executed trade with price, size, and timestamp.
    /// </summary>
    Trade = 2
}
