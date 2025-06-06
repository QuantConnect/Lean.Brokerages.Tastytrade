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
/// Represents a futures option contract with details such as expiration, strike price, and option type.
/// </summary>
public sealed class FutureOption : BaseInstrument
{
    /// <summary>
    /// Gets the expiration date of the option contract.
    /// </summary>
    public DateTime ExpirationDate { get; set; }

    /// <summary>
    /// Gets the type of the option (e.g., Call or Put).
    /// </summary>
    public OptionType OptionType { get; set; }

    /// <summary>
    /// Gets the strike price of the option.
    /// </summary>
    public decimal StrikePrice { get; set; }

    /// <summary>
    /// Determines whether this option matches the specified expiration date, strike price, and option type.
    /// </summary>
    /// <param name="expirationDate">The expiration date to match.</param>
    /// <param name="strike">The strike price to match.</param>
    /// <param name="optionType">The option type to match.</param>
    /// <returns><c>true</c> if this option matches all provided criteria; otherwise, <c>false</c>.</returns>
    public bool IsMatchFor(DateTime expirationDate, decimal strike, OptionType optionType)
    {
        return ExpirationDate.Date == expirationDate.Date &&
               StrikePrice == strike &&
               OptionType == optionType;
    }

    /// <summary>
    /// Returns a string representation of the <see cref="FutureOption"/> instance,
    /// including the symbol, option type, expiration date, strike price, and streamer symbol.
    /// </summary>
    /// <returns>
    /// A human-readable <see cref="string"/> that describes the key properties of the option.
    /// </returns>
    public override string ToString()
    {
        return $"{Symbol} | {OptionType} | Exp: {ExpirationDate:yyyy-MM-dd} | Strike: {StrikePrice} | Streamer: {StreamerSymbol}";
    }
}
