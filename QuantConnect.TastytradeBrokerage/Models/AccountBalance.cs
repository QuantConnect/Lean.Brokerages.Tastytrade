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

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Represents the balance details of a trading account, including available funds, cash, and currency.
/// </summary>
public class AccountBalance
{
    /// <summary>
    /// Gets the amount of funds available for trading.
    /// </summary>
    public decimal AvailableTradingFunds { get; set; }

    /// <summary>
    /// Gets the total cash balance in the account.
    /// </summary>
    public decimal CashBalance { get; set; }

    /// <summary>
    /// Gets the amount of cash available after settlement.
    /// </summary>
    public decimal CashSettleBalance { get; set; }

    /// <summary>
    /// Gets the currency code of the account (e.g., USD).
    /// </summary>
    public string Currency { get; set; }
}
