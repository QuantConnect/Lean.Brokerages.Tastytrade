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
    public string AccountNumber { get; set; }

    /// <summary>
    /// Symbol of the position.
    /// </summary>
    /// <example>./ESZ4 EW3U4 240920P5650</example>
    public string Symbol { get; set; }

    /// <summary>
    /// The instrument type of the position.
    /// Values: Equity, Equity Option, Future, Future Option, Cryptocurrency.
    /// </summary>
    /// <example>Future Option</example>
    public InstrumentType InstrumentType { get; set; }

    /// <summary>
    /// The symbol of the underlying instrument, if applicable.
    /// </summary>
    public string UnderlyingSymbol { get; set; }

    /// <summary>
    /// The quantity of your position. Some stocks can be traded in fractional quantities.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Indicates the side or direction of the position. Zero means the position is closed.
    /// </summary>
    public Direction QuantityDirection { get; set; }

    /// <summary>
    /// Price of the instrument at market close yesterday.
    /// </summary>
    public decimal ClosePrice { get; set; }

    /// <summary>
    /// A running average of the open price of the position. Cost basis for unrealized gain since open calculation.
    /// </summary>
    public decimal AverageOpenPrice { get; set; }

    /// <summary>
    /// Cost basis for unrealized year gain calculation.
    /// </summary>
    public decimal AverageYearlyMarketClosePrice { get; set; }

    /// <summary>
    /// Cost basis for unrealized day gain calculation.
    /// </summary>
    public decimal AverageDailyMarketClosePrice { get; set; }

    /// <summary>
    /// Indicates the notional multiplier of the position based on what is delivered if the position gets exercised/assigned.
    /// </summary>
    /// <example>equity options usually have a multiplier of `100`, meaning the option contract delivers 100 shares upon exercise.</example>
    public decimal Multiplier { get; set; }

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
    public bool IsSuppressed { get; set; }

    /// <summary>
    /// Indicates the rare case when an admin has taken action to freeze this position. Tastytrade will do this to protect a compromised account. Frozen positions are not adjustable/tradeable.
    /// </summary>
    public bool IsFrozen { get; set; }

    /// <summary>
    /// The quantity that cannot be traded or modified due to something like an expected assignment
    /// </summary>
    public decimal RestrictedQuantity { get; set; }

    /// <summary>
    /// An aggregate amount of profit or loss on a realized (already closed) position for the current trading day. This number is based on the position’s opening mark for the day.
    /// </summary>
    public decimal RealizedDayGain { get; set; }

    /// <summary>
    /// The direction of the realized day gain. Credit means positive gain. Debit means loss.
    /// Values: Credit, Debit, None.
    /// </summary>
    public string RealizedDayGainEffect { get; set; }

    /// <summary>
    /// Indicates the date of realized day gain.
    /// </summary>
    public DateTime RealizedDayGainDate { get; set; }

    /// <summary>
    /// The total profit or loss realized from a position since it was opened.
    /// </summary>
    public decimal RealizedToday { get; set; }

    /// <summary>
    /// The direction of the realized today value. Credit means positive gain. Debit means loss.
    /// Values: Credit (gain), Debit (loss), None.
    /// </summary>
    public string RealizedTodayEffect { get; set; }

    /// <summary>
    /// Indicates the date of the realized today value.
    /// </summary>
    public DateTime RealizedTodayDate { get; set; }

    /// <summary>
    /// The date and time the position was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The date and time the position was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Returns a string that represents the current position.
    /// </summary>
    public override string ToString()
    {
        return $"Position [Account={AccountNumber}, Symbol={Symbol}, InstrumentType={InstrumentType}, Quantity={Quantity}, Direction={QuantityDirection}, " +
               $"AvgOpenPrice={AverageOpenPrice}, ClosePrice={ClosePrice}, RealizedToday={RealizedToday} {RealizedTodayEffect}, " +
               $"RealizedDayGain={RealizedDayGain} {RealizedDayGainEffect}, CreatedAt={CreatedAt:yyyy-MM-dd}, UpdatedAt={UpdatedAt:yyyy-MM-dd}]";
    }
}
