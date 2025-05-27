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
/// This is the "side" you are taking (buy or sell) combined with an "opening" or "closing" designation.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum OrderAction
{
    /// <summary>
    /// Only applies to single leg outright futures trades.
    /// This action allows you to buy an outright future regardless of your position.
    /// If you are short an outright, this will result in closing your position.
    /// </summary>
    Buy = 0,

    /// <summary>
    /// Only applies to single leg outright futures trades.
    /// This action allows you to sell an outright future regardless of your position.
    /// If you are long an outright, this will result in closing your position.
    /// </summary>
    Sell = 1,

    /// <summary>
    /// Open a long position by buying the instrument.
    /// You must <b>not</b> have an existing <b>short</b> position in that instrument.
    /// </summary>
    [EnumMember(Value = "Buy to Open")]
    BuyToOpen = 2,

    /// <summary>
    /// Open a short position by selling the instrument.
    /// You must <b>not</b> have an existing <b>long</b> position in that instrument.
    /// </summary>
    [EnumMember(Value = "Sell to Open")]
    SellToOpen = 3,

    /// <summary>
    /// Close a short position by buying the instrument.
    /// You must have an existing short position in that instrument.
    /// </summary>
    [EnumMember(Value = "Buy to Close")]
    BuyToClose = 4,

    /// <summary>
    /// Close a long position by selling the instrument.
    /// You <b>must</b> have an existing <b>long</b> position in that instrument.
    /// </summary>
    [EnumMember(Value = "Sell to Close")]
    SellToClose = 5,
}
