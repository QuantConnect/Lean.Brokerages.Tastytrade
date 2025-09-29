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
/// Represents the response returned after placing or retrieving an order.
/// </summary>
public class OrderResponse
{
    /// <summary>
    /// Gets the order details.
    /// </summary>
    public Order Order { get; set; }
}

/// <summary>
/// Represents a financial order containing legs and status.
/// </summary>
public class Order
{
    /// <summary>
    /// Gets the unique identifier for the order.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Expire date when <see cref="TimeInForce.GoodTilDate"/>
    /// </summary>
    public DateTime GtcDate { get; set; }

    /// <summary>
    /// Gets the date and time when the order was cancelled.
    /// </summary>
    public DateTimeOffset CancelledAt { get; set; }

    /// <summary>
    /// Gets the UTC representation of the <see cref="CancelledAt"/>.
    /// </summary>
    public DateTime CancelledAtUtc => CancelledAt.UtcDateTime;

    /// <summary>
    /// Gets the current type of the order.
    /// </summary>
    public OrderType OrderType { get; set; }

    /// <summary>
    /// Gets the limit price associated with the order.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// The price impact of the order (positive for credit, negative for debit).
    /// </summary>
    public PriceEffect PriceEffect { get; init; }

    /// <summary>
    /// Gets the date and time when the order was received.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>
    /// Gets the UTC representation of the <see cref="ReceivedAtUtc"/>.
    /// </summary>
    public DateTime ReceivedAtUtc => ReceivedAt.UtcDateTime;

    /// <summary>
    /// Gets the current status of the order.
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Gets the stop price associated with the order.
    /// </summary>
    public decimal StopTrigger { get; set; }

    /// <summary>
    /// Gets the time-in-force designation for the order.
    /// </summary>
    public TimeInForce TimeInForce { get; set; }

    /// <summary>
    /// Gets the symbol of the underlying security for the order.
    /// </summary>
    public string UnderlyingSymbol { get; set; }

    /// <summary>
    /// Gets the collection of legs that make up the order.
    /// </summary>
    public IReadOnlyCollection<Leg> Legs { get; set; }

    /// <summary>
    /// Returns a string that represents the current order.
    /// </summary>
    public override string ToString()
    {
        return $"Order ID: {Id}, Type: {OrderType}, Price: {Price}, Price Effect: {PriceEffect}, ReceivedAt: {ReceivedAt:yyyy-MM-dd HH:mm:ss}, Status: {Status}, TIF: {TimeInForce}, Underlying: {UnderlyingSymbol}, Legs: [{string.Join("; ", Legs)}]";
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
    public OrderAction Action { get; set; }

    /// <summary>
    /// Gets the type of instrument for the leg.
    /// </summary>
    public InstrumentType InstrumentType { get; set; }

    /// <summary>
    /// Gets the total quantity for this leg.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets the remaining quantity that has not yet been filled.
    /// </summary>
    public decimal RemainingQuantity { get; set; }

    /// <summary>
    /// Gets the symbol for the instrument in this leg.
    /// </summary>
    public string Symbol { get; set; }

    /// <summary>
    /// Gets the fills associated with this leg.
    /// </summary>
    public IReadOnlyCollection<Fill> Fills { get; set; }

    /// <summary>
    /// Returns a string that represents the current leg.
    /// </summary>
    public override string ToString()
    {
        return $"Action: {Action}, Type: {InstrumentType}, Symbol: {Symbol}, Quantity: {Quantity}, Remaining: {RemainingQuantity}, Fills: [{string.Join("; ", Fills)}]";
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
    public decimal FillPrice { get; set; }

    /// <summary>
    /// Gets the timestamp when the fill occurred.
    /// </summary>
    public DateTime FilledAt { get; set; }

    /// <summary>
    /// Gets the quantity that was filled.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Returns a string that represents the current fill.
    /// </summary>
    public override string ToString()
    {
        return $"Qty: {Quantity} @ {FillPrice} on {FilledAt:yyyy-MM-dd HH:mm:ss}";
    }
}
