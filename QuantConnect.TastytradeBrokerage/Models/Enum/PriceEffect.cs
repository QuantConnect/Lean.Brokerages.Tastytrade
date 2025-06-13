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

namespace QuantConnect.Brokerages.Tastytrade.Models.Enum;

/// <summary>
/// Specifies whether the transaction will result in a credit or debit
/// to the customer's account based on the order action.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum PriceEffect
{
    /// <summary>
    /// The price is credited to the customer's account.
    /// </summary>
    /// <remarks>
    /// Indicates that the transaction results in receiving funds.
    /// Typically used with <see cref="OrderAction.SellToOpen"/> and <see cref="OrderAction.SellToClose"/>.
    /// </remarks>
    Credit = 0,

    /// <summary>
    /// The price is debited from the customer's account.
    /// </summary>
    /// <remarks>
    /// Indicates that the transaction results in a payment.
    /// Typically used with <see cref="OrderAction.BuyToOpen"/> and <see cref="OrderAction.BuyToClose"/>.
    /// </remarks>
    Debit = 1,
}
