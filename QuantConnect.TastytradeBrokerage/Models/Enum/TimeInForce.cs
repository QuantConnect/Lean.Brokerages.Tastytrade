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
using System.Runtime.Serialization;

namespace QuantConnect.Brokerages.Tastytrade.Models.Enum;

/// <summary>
/// Represents the duration of an order in the Tastytrade trading system.
/// </summary>
/// <remarks>Time in force means "How long do I want this order to live before it expires?"</remarks>
[JsonConverter(typeof(StringEnumConverter))]
public enum TimeInForce
{
    /// <summary>
    /// Order will work until filled or the market closes.
    /// </summary>
    Day = 0,

    /// <summary>
    /// Order will work until filled or the customer cancels.
    /// </summary>
    [EnumMember(Value = "GTC")]
    GoodTillCancel = 1,

    /// <summary>
    /// Order will work until filled or a given date. Orders must also include 'gtc-date' parameter for GTD orders.
    /// </summary>
    [EnumMember(Value = "GTD")]
    GoodTilDate = 2
}
