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
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Models.Orders;

/// <summary>
/// Represents the response returned after placing or retrieving an order.
/// </summary>
public class OrderResponse
{
    /// <summary>
    /// Gets the order details.
    /// </summary>
    public Order Order { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderResponse"/> class.
    /// </summary>
    /// <param name="order">The order returned in the response.</param>
    public OrderResponse(Order order) => Order = order;
}

/// <summary>
/// Represents a financial order containing legs and status.
/// </summary>
public class Order
{
    /// <summary>
    /// Gets the unique identifier for the order.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the current status of the order.
    /// </summary>
    public OrderStatus Status { get; }

    /// <summary>
    /// Gets the collection of legs that make up the order.
    /// </summary>
    public IReadOnlyCollection<Leg> Legs { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Order"/> class.
    /// </summary>
    /// <param name="id">The unique order ID.</param>
    /// <param name="status">The status of the order.</param>
    /// <param name="legs">The collection of legs for the order.</param>
    [JsonConstructor]
    public Order(string id, OrderStatus status, IReadOnlyCollection<Leg> legs)
    {
        Id = id;
        Status = status;
        Legs = legs;
    }
}

/// <summary>
/// Represents a leg of an order, typically part of a multi-leg order.
/// </summary>
public class Leg
{
    /// <summary>
    /// Gets the action associated with the leg (e.g., Buy or Sell).
    /// </summary>
    public OrderAction Action { get; }

    /// <summary>
    /// Gets the total quantity for this leg.
    /// </summary>
    public decimal Quantity { get; }

    /// <summary>
    /// Gets the remaining quantity that has not yet been filled.
    /// </summary>
    public decimal RemainingQuantity { get; }

    /// <summary>
    /// Gets the fills associated with this leg.
    /// </summary>
    public IReadOnlyCollection<Fill> Fills { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Leg"/> class.
    /// </summary>
    /// <param name="action">The order action for the leg.</param>
    /// <param name="quantity">The total quantity of the leg.</param>
    /// <param name="remainingQuantity">The remaining unfilled quantity.</param>
    /// <param name="fills">The fills associated with the leg.</param>
    [JsonConstructor]
    public Leg(OrderAction action, decimal quantity, decimal remainingQuantity, IReadOnlyCollection<Fill> fills)
    {
        Action = action;
        Quantity = quantity;
        RemainingQuantity = remainingQuantity;
        Fills = fills;
    }
}

/// <summary>
/// Represents a fill of a leg in an order.
/// </summary>
public class Fill
{
    /// <summary>
    /// Gets the price at which the quantity was filled.
    /// </summary>
    public decimal FillPrice { get; }

    /// <summary>
    /// Gets the timestamp when the fill occurred.
    /// </summary>
    public DateTime FilledAt { get; }

    /// <summary>
    /// Gets the quantity that was filled.
    /// </summary>
    public decimal Quantity { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Fill"/> class.
    /// </summary>
    /// <param name="fillPrice">The fill price.</param>
    /// <param name="filledAt">The date and time of the fill.</param>
    /// <param name="quantity">The quantity filled.</param>
    public Fill(decimal fillPrice, DateTime filledAt, decimal quantity)
    {
        FillPrice = fillPrice;
        FilledAt = filledAt;
        Quantity = quantity;
    }
}
