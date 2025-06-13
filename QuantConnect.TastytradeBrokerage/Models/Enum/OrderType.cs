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
/// Defines the various types of orders supported by the Tastytrade brokerage.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum OrderType
{
    /// <summary>
    /// A limit order executes at a specified price or better.
    /// </summary>
    /// <remarks>
    /// Requirements:
    /// <list type="bullet">
    /// <item><description>Must include <b>price</b> and <b>price-effect</b> in the JSON.</description></item>
    /// </list>
    /// </remarks>
    Limit = 0,

    /// <summary>
    /// A market order executes immediately at the best available price.
    /// </summary>
    /// <remarks>
    /// Restrictions:
    /// <list type="bullet">
    /// <item><description>Must have exactly one leg.</description></item>
    /// <item><description>Must <b>not</b> include <b>price</b> or <b>price-effect</b>.</description></item>
    /// <item><description><b>time-in-force</b> must <b>not</b> be <b>GTC</b>.</description></item>
    /// <item><description>Opening market orders cannot be placed while the market is closed.</description></item>
    /// </list>
    /// </remarks>
    Market = 1,

    /// <summary>
    /// A stop order becomes a market order once a specified stop-trigger price is reached.
    /// </summary>
    /// <remarks>
    /// Restrictions:
    /// <list type="bullet">
    /// <item><description>Must <b>not</b> include <b>price</b> or <b>price-effect</b>.</description></item>
    /// <item><description>Must include a <b>stop-trigger</b> price.</description></item>
    /// <item><description><b>time-in-force</b> must <b>not</b> be <b>GTC</b>.</description></item>
    /// </list>
    /// Note: "Stop" and "Stop Market" refer to the same concept, but only the value <c>Stop</c> is accepted in the JSON.
    /// </remarks>
    Stop = 2,

    /// <summary>
    /// A stop limit order becomes a limit order once a specified stop-trigger price is reached.
    /// </summary>
    /// <remarks>
    /// Requirements:
    /// <list type="bullet">
    /// <item><description>Must include <b>price</b> or <b>price-effect</b>.</description></item>
    /// <item><description>Must include a <b>stop-trigger</b> price.</description></item>
    /// </list>
    /// </remarks>
    [EnumMember(Value = "Stop Limit")]
    StopLimit = 3,

    /// <summary>
    /// A notional market order specifies a dollar value instead of a quantity.
    /// </summary>
    /// <example>Buy $10 worth of AAPL instead of 1 share.</example>
    /// <remarks>
    /// JSON structure rules:
    /// <list type="bullet">
    /// <item><description><b>order-type</b> must be <b>Notional Market</b>.</description></item>
    /// <item><description>Must have exactly one leg.</description></item>
    /// <item><description>Order legs must <b>not</b> include <b>quantity</b>.</description></item>
    /// <item><description>Must include <b>value</b> and <b>value-effect</b>.</description></item>
    /// <item><description>Only supported for <see cref="InstrumentType.Cryptocurrency"/> and eligible <see cref="InstrumentType.Equity"/> symbols.</description></item>
    /// </list>
    /// </remarks>
    [EnumMember(Value = "Notional Market")]
    NotionalMarket = 4,
}
