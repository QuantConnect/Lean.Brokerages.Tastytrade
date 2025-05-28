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
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Models.Orders;

/// <summary>
/// Represents a stop market order that becomes a market order once the stop price is reached.
/// </summary>
public class StopMarketOrderRequest : OrderBaseRequest
{
    /// <summary>
    /// Gets the order type (Stop).
    /// </summary>
    public override OrderType OrderType => OrderType.Stop;

    /// <summary>
    /// Initializes a new instance of a <see cref="StopMarketOrderRequest"/>.
    /// </summary>
    /// <param name="timeInForce">Time in force for the order.</param>
    /// <param name="expiryDateTime">Expiration date if <paramref name="timeInForce"/> is GTC.</param>
    /// <param name="legs">The order legs to execute.</param>
    /// <param name="stopPrice">The stop trigger price for the order.</param>
    public StopMarketOrderRequest(TimeInForce timeInForce, DateTime? expiryDateTime, IReadOnlyCollection<LegAttributes> legs, decimal stopPrice)
        : base(timeInForce, expiryDateTime, legs)
    {
        StopPrice = stopPrice;
    }
}
