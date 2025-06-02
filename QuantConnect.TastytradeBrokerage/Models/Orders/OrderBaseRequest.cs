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
using QuantConnect.Brokerages.Tastytrade.Serialization;
using LeanOrderDirection = QuantConnect.Orders.OrderDirection;

namespace QuantConnect.Brokerages.Tastytrade.Models.Orders;

/// <summary>
/// Represents the base class for all brokerage order request types.
/// </summary>
public abstract class OrderBaseRequest
{
    /// <summary>
    /// Gets the time in force (e.g., Day, GTC) for the order.
    /// </summary>
    public TimeInForce TimeInForce { get; }

    /// <summary>
    /// Gets the expiration date for the order if <see cref="TimeInForce"/> is GoodTilDate.
    /// </summary>
    [JsonProperty("gtc-date")]
    public DateTime? ExpiryDateTime { get; }

    /// <summary>
    /// Gets the type of the order (e.g., Market, Limit, Stop).
    /// </summary>
    public abstract OrderType OrderType { get; }

    /// <summary>
    /// Gets the legs associated with this order.
    /// </summary>
    public IReadOnlyCollection<LegAttributes> Legs { get; }

    /// <summary>
    /// Gets the optional stop price used for stop or stop-limit orders.
    /// </summary>
    [JsonProperty("stop-trigger")]
    public decimal? StopPrice { get; protected set; }

    /// <summary>
    /// Gets the price effect (debit or credit) of the order.
    /// </summary>
    public PriceEffect? PriceEffect { get; protected set; }

    /// <summary>
    /// Gets the limit price for applicable orders.
    /// </summary>
    public decimal? Price { get; protected set; }

    /// <summary>
    /// It allows Tastytrade team to better identify all QuantConnect orders coming through for the best service when need and reporting tools.
    /// </summary>
    public string Source => "QuantConnect";

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderBaseRequest"/> class without specifying a price effect.
    /// This constructor is typically used for orders that do not require a directional price impact (e.g., raw market orders).
    /// </summary>
    /// <param name="timeInForce">
    /// The time in force policy for the order, such as <see cref="TimeInForce.Day"/> or <see cref="TimeInForce.GoodTilDate"/>.
    /// </param>
    /// <param name="expiryDateTime">
    /// The expiration date for the order. This must be provided if <paramref name="timeInForce"/> is <see cref="TimeInForce.GoodTilDate"/>.
    /// </param>
    /// <param name="legs">
    /// A collection of <see cref="LegAttributes"/> defining the individual legs of the order (e.g., quantity, symbol, action).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="timeInForce"/> is <see cref="TimeInForce.GoodTilDate"/> and <paramref name="expiryDateTime"/> is null.
    /// </exception>
    protected OrderBaseRequest(TimeInForce timeInForce, DateTime? expiryDateTime, IReadOnlyCollection<LegAttributes> legs)
    {
        Legs = legs;
        TimeInForce = timeInForce;
        if (timeInForce == TimeInForce.GoodTilDate && !expiryDateTime.HasValue)
        {
            throw new ArgumentNullException(nameof(expiryDateTime), $"An expiry date must be provided when using {TimeInForce.GoodTilDate}.");
        }
        ExpiryDateTime = expiryDateTime;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderBaseRequest"/> class and sets the <see cref="PriceEffect"/>
    /// based on the specified LEAN order direction.
    /// This constructor should be used when the order type requires identifying whether funds will be debited or credited.
    /// </summary>
    /// <param name="timeInForce">
    /// The time in force policy for the order, such as <see cref="TimeInForce.Day"/> or <see cref="TimeInForce.GoodTilDate"/>.
    /// </param>
    /// <param name="expiryDateTime">
    /// The expiration date for the order. This must be provided if <paramref name="timeInForce"/> is <see cref="TimeInForce.GoodTilDate"/>.
    /// </param>
    /// <param name="legs">
    /// A collection of <see cref="LegAttributes"/> defining the individual legs of the order (e.g., quantity, symbol, action).
    /// </param>
    /// <param name="leanOrderDirection">
    /// The directional intent of the order from the LEAN engine (<see cref="LeanOrderDirection.Buy"/> or <see cref="LeanOrderDirection.Sell"/>).
    /// Determines whether the <see cref="PriceEffect"/> is <see cref="PriceEffect.Debit"/> or <see cref="PriceEffect.Credit"/>.
    /// </param>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="leanOrderDirection"/> is not a supported direction (Buy/Sell).
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="timeInForce"/> is <see cref="TimeInForce.GoodTilDate"/> and <paramref name="expiryDateTime"/> is null.
    /// </exception>
    protected OrderBaseRequest(TimeInForce timeInForce, DateTime? expiryDateTime, IReadOnlyCollection<LegAttributes> legs, LeanOrderDirection leanOrderDirection)
        : this(timeInForce, expiryDateTime, legs)
    {
        PriceEffect = leanOrderDirection switch
        {
            LeanOrderDirection.Buy => Enum.PriceEffect.Debit,
            LeanOrderDirection.Sell => Enum.PriceEffect.Credit,
            _ => throw new NotSupportedException($"The order direction '{leanOrderDirection}' is not supported for conversion to PriceEffect.")
        };
    }

    /// <summary>
    /// Serializes this <see cref="OrderBaseRequest"/> instance to a JSON string
    /// using kebab-case naming for properties as defined in <see cref="JsonSettings.KebabCase"/>.
    /// </summary>
    /// <returns>A kebab-case JSON string representing the current message.</returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, JsonSettings.KebabCase);
    }
}
