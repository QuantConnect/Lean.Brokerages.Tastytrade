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
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Represents a financial position within an account, such as a stock, option, or future.
/// A position with a quantity of 0 is considered closed. These positions are purged overnight.
/// </summary>
public class Position
{
    /// <summary>
    /// The account number.
    /// </summary>
    //[JsonProperty("account-number")]
    public string AccountNumber { get; }

    /// <summary>
    /// Symbol of the position.
    /// </summary>
    /// <example>./ESZ4 EW3U4 240920P5650</example>
    public string Symbol { get; }

    /// <summary>
    /// The instrument type of the position.
    /// Values: Equity, Equity Option, Future, Future Option, Cryptocurrency.
    /// </summary>
    /// <example>Future Option</example>
    //[JsonProperty("instrument-type")]
    public string InstrumentType { get; }

    /// <summary>
    /// The symbol of the underlying instrument, if applicable.
    /// </summary>
    public string UnderlyingSymbol { get; }

    /// <summary>
    /// The quantity of your position. Some stocks can be traded in fractional quantities.
    /// </summary>
    public decimal Quantity { get; }

    /// <summary>
    /// Indicates the side or direction of the position. Zero means the position is closed.
    /// </summary>
    public Direction QuantityDirection { get; }

    /// <summary>
    /// Price of the instrument at market close yesterday.
    /// </summary>
    public decimal ClosePrice { get; }

    /// <summary>
    /// A running average of the open price of the position. Cost basis for unrealized gain since open calculation.
    /// </summary>
    public decimal AverageOpenPrice { get; }

    /// <summary>
    /// Cost basis for unrealized year gain calculation.
    /// </summary>
    public decimal AverageYearlyMarketClosePrice { get; }

    /// <summary>
    /// Cost basis for unrealized day gain calculation.
    /// </summary>
    public decimal AverageDailyMarketClosePrice { get; }

    /// <summary>
    /// Indicates the notional multiplier of the position based on what is delivered if the position gets exercised/assigned.
    /// </summary>
    /// <example>equity options usually have a multiplier of `100`, meaning the option contract delivers 100 shares upon exercise.</example>
    public int Multiplier { get; }

    /// <summary>
    /// A tastytrade-specific value to categorize the cost of the position.
    /// Values: Credit, Debit, None.
    /// </summary>
    public string CostEffect { get; }

    /// <summary>
    /// This field is not in use anymore and can be ignored.
    /// <see href="https://developer.tastytrade.com/api-guides/account-positions/"/>
    /// </summary>
    [Obsolete("Tastytrade docs: This field is not in use anymore and can be ignored")]
    public bool IsSuppressed { get; }

    /// <summary>
    /// Indicates the rare case when an admin has taken action to freeze this position. Tastytrade will do this to protect a compromised account. Frozen positions are not adjustable/tradeable.
    /// </summary>
    public bool IsFrozen { get; }

    /// <summary>
    /// The quantity that cannot be traded or modified due to something like an expected assignment
    /// </summary>
    public decimal RestrictedQuantity { get; }

    /// <summary>
    /// An aggregate amount of profit or loss on a realized (already closed) position for the current trading day. This number is based on the position’s opening mark for the day.
    /// </summary>
    public decimal RealizedDayGain { get; }

    /// <summary>
    /// The direction of the realized day gain. Credit means positive gain. Debit means loss.
    /// Values: Credit, Debit, None.
    /// </summary>
    public string RealizedDayGainEffect { get; }

    /// <summary>
    /// Indicates the date of realized day gain.
    /// </summary>
    public DateTime RealizedDayGainDate { get; }

    /// <summary>
    /// The total profit or loss realized from a position since it was opened.
    /// </summary>
    public decimal RealizedToday { get; }

    /// <summary>
    /// The direction of the realized today value. Credit means positive gain. Debit means loss.
    /// Values: Credit (gain), Debit (loss), None.
    /// </summary>
    public string RealizedTodayEffect { get; }

    /// <summary>
    /// Indicates the date of the realized today value.
    /// </summary>
    public DateTime RealizedTodayDate { get; }

    /// <summary>
    /// The date and time the position was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// The date and time the position was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Position"/> class with the specified property values.
    /// </summary>
    /// <param name="accountNumber">The account number.</param>
    /// <param name="symbol">Symbol of the position.</param>
    /// <param name="instrumentType">The type of instrument (e.g., Equity, Option, Future).</param>
    /// <param name="underlyingSymbol">The symbol of the underlying instrument, if applicable.</param>
    /// <param name="quantity">The quantity of the position. Zero indicates a closed position.</param>
    /// <param name="quantityDirection">The side/direction of the position (e.g., long or short).</param>
    /// <param name="closePrice">The instrument's market close price from the previous day.</param>
    /// <param name="averageOpenPrice">The average open price used as cost basis for unrealized gains.</param>
    /// <param name="averageYearlyMarketClosePrice">The yearly close price used for unrealized year gain calculations.</param>
    /// <param name="averageDailyMarketClosePrice">The daily close price used for unrealized day gain calculations.</param>
    /// <param name="multiplier">The notional multiplier (e.g., 100 for equity options).</param>
    /// <param name="costEffect">Categorization of the position's cost (Credit, Debit, None).</param>
    /// <param name="isFrozen">Indicates if the position is frozen due to administrative action.</param>
    /// <param name="restrictedQuantity">The portion of the position that is not tradable or modifiable.</param>
    /// <param name="realizedDayGain">Realized profit/loss for the current trading day.</param>
    /// <param name="realizedDayGainEffect">The direction of the realized day gain (Credit, Debit, None).</param>
    /// <param name="realizedDayGainDate">The date of the realized day gain.</param>
    /// <param name="realizedToday">Total realized profit/loss since position was opened.</param>
    /// <param name="realizedTodayEffect">The direction of the realized today value (Credit, Debit, None).</param>
    /// <param name="realizedTodayDate">The date of the realized today value.</param>
    /// <param name="createdAt">The date and time the position was created.</param>
    /// <param name="updatedAt">The date and time the position was last updated.</param>
    [JsonConstructor]
    public Position(string accountNumber, string symbol, string instrumentType, string underlyingSymbol, decimal quantity, Direction quantityDirection, decimal closePrice, decimal averageOpenPrice, decimal averageYearlyMarketClosePrice, decimal averageDailyMarketClosePrice, int multiplier, string costEffect, bool isFrozen, decimal restrictedQuantity, decimal realizedDayGain, string realizedDayGainEffect, DateTime realizedDayGainDate, decimal realizedToday, string realizedTodayEffect, DateTime realizedTodayDate, DateTime createdAt, DateTime updatedAt)
    {
        AccountNumber = accountNumber;
        Symbol = symbol;
        InstrumentType = instrumentType;
        UnderlyingSymbol = underlyingSymbol;
        Quantity = quantity;
        QuantityDirection = quantityDirection;
        ClosePrice = closePrice;
        AverageOpenPrice = averageOpenPrice;
        AverageYearlyMarketClosePrice = averageYearlyMarketClosePrice;
        AverageDailyMarketClosePrice = averageDailyMarketClosePrice;
        Multiplier = multiplier;
        CostEffect = costEffect;
        IsFrozen = isFrozen;
        RestrictedQuantity = restrictedQuantity;
        RealizedDayGain = realizedDayGain;
        RealizedDayGainEffect = realizedDayGainEffect;
        RealizedDayGainDate = realizedDayGainDate;
        RealizedToday = realizedToday;
        RealizedTodayEffect = realizedTodayEffect;
        RealizedTodayDate = realizedTodayDate;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}
