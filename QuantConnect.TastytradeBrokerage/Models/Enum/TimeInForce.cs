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
using System.Runtime.Serialization;
using QuantConnect.Brokerages.Tastytrade.Serialization;

namespace QuantConnect.Brokerages.Tastytrade.Models.Enum;

/// <summary>
/// Represents the duration of an order in the Tastytrade trading system.
/// </summary>
/// <remarks>Time in force means "How long do I want this order to live before it expires?"</remarks>
[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum TimeInForce
{
    /// <summary>
    /// A duration returned by the brokerage that this library does not support yet.
    /// </summary>
    /// <remarks>The unrecognized value is logged verbatim by <see cref="TolerantStringEnumConverter"/>.</remarks>
    Unknown = -1,

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
    GoodTilDate = 2,

    /// <summary>
    /// Order will work during the extended trading hours session until filled or the session closes.
    /// </summary>
    [EnumMember(Value = "Ext")]
    DayExtendedHours = 3,

    /// <summary>
    /// Order will work during extended trading hours until filled or the customer cancels.
    /// </summary>
    [EnumMember(Value = "GTC Ext")]
    GoodTillCancelExtendedHours = 4,

    /// <summary>
    /// Order will work during the overnight trading session until filled or the session closes.
    /// </summary>
    [EnumMember(Value = "Ext Overnight")]
    OvernightExtendedHours = 5,

    /// <summary>
    /// Order will be filled immediately, in whole or in part, and any remaining quantity is cancelled.
    /// </summary>
    [EnumMember(Value = "IOC")]
    ImmediateOrCancel = 6
}
