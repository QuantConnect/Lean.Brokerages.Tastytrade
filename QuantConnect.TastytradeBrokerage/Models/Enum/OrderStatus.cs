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
/// Represents the current status of an order within the trading system.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum OrderStatus
{
    /// <summary>
    /// The order status is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The order has been received by the system.
    /// </summary>
    Received = 1,

    /// <summary>
    /// The order is being sent out of the tastytrade system.
    /// </summary>
    Routed = 2,

    /// <summary>
    /// Order is en route to the exchange.
    /// </summary>
    [EnumMember(Value = "In Flight")]
    InFlight = 3,

    /// <summary>
    /// The order is live and active at the exchange.
    /// </summary>
    Live = 4,

    /// <summary>
    /// Customer has requested to cancel the order. Awaiting a 'cancelled' message from the exchange.
    /// </summary>
    [EnumMember(Value = "Cancel Requested")]
    CancelRequested = 5,

    /// <summary>
    /// Customer has submitted a replacement order. This order is awaiting a 'cancelled' message from the exchange.
    /// </summary>
    [EnumMember(Value = "Replace Requested")]
    ReplaceRequested = 6,

    /// <summary>
    /// This means the order is awaiting a status update of a related order. This pertains to replacement orders, complex OTOCO orders, and complex OTO orders.
    /// </summary>
    Contingent = 7,

    /// <summary>
    /// Order has been fully filled.
    /// </summary>
    Filled = 8,

    /// <summary>
    /// Order is cancelled.
    /// </summary>
    Cancelled = 9,

    /// <summary>
    /// Order has expired. Usually applies to an option order.
    /// </summary>
    Expired = 10,

    /// <summary>
    /// Order has been rejected by either tastytrade or the exchange.
    /// </summary>
    Rejected = 11,

    /// <summary>
    /// Administrator has manually removed this order from customer account.
    /// </summary>
    Removed = 12,

    /// <summary>
    /// Administrator has manually removed part of this order from customer account.
    /// </summary>
    [EnumMember(Value = "Partially Removed")]
    PartiallyRemoved = 13,
}
