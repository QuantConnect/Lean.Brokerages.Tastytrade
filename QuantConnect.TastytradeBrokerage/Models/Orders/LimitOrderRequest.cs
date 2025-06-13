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
using LeanOrderDirection = QuantConnect.Orders.OrderDirection;

namespace QuantConnect.Brokerages.Tastytrade.Models.Orders;

/// <summary>
/// Represents a limit order that executes at a specified price or better.
/// </summary>
public class LimitOrderRequest : OrderBaseRequest
{
    /// <summary>
    /// Gets the order type (Limit).
    /// </summary>
    public override OrderType OrderType => OrderType.Limit;

    /// <summary>
    /// Initializes a new instance of a <see cref="LimitOrderRequest"/>.
    /// </summary>
    /// <param name="timeInForce">Time in force for the order.</param>
    /// <param name="expiryDateTime">Expiration date if <paramref name="timeInForce"/> is GTC.</param>
    /// <param name="legs">The order legs to execute.</param>
    /// <param name="price">The limit price for the order.</param>
    /// <param name="leanOrderDirection">Direction of the order (Buy/Sell).</param>
    public LimitOrderRequest(TimeInForce timeInForce, DateTime? expiryDateTime, IReadOnlyCollection<LegAttributes> legs, decimal price, LeanOrderDirection leanOrderDirection)
        : base(timeInForce, expiryDateTime, legs, leanOrderDirection)
    {
        Price = price;
    }
}
