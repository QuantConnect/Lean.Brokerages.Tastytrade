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

using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Models.Orders;

/// <summary>
/// Represents the attributes of a single order leg. 
/// Each leg specifies an individual instrument (such as a stock or option), its quantity, and the action taken (e.g., buy or sell).
/// </summary>
/// <remarks>
/// - All orders must include at least one leg.
/// - The maximum number of legs in an order depends on the instrument type:
/// <list type="bullet">
/// <item>
///   <term>Equity, Futures, and Cryptocurrency</term>
///   <description>Orders are limited to a single leg.</description>
/// </item>
/// <item>
///   <term>Equity Options and Futures Options</term>
///   <description>Orders can contain up to four legs.</description>
/// </item>
/// </list>
/// </remarks>
public class LegAttributes
{
    /// <summary>
    /// The action being taken on the instrument (e.g., Buy or Sell).
    /// </summary>
    public OrderAction Action { get; }

    /// <summary>
    /// The type or class of the instrument (e.g., Equity, Option, Future).
    /// </summary>
    public InstrumentType InstrumentType { get; }

    /// <summary>
    /// The quantity of the instrument to buy or sell.
    /// </summary>
    /// <remarks>Required unless the order type is Notional Market.</remarks>
    public decimal Quantity { get; }

    /// <summary>
    /// The ticker symbol or identifier of the instrument.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LegAttributes"/> class with the specified order leg parameters.
    /// </summary>
    /// <param name="action">The action to perform on the instrument (Buy/Sell).</param>
    /// <param name="instrumentType">The class/type of the instrument.</param>
    /// <param name="quantity">The number of units involved in the transaction.</param>
    /// <param name="symbol">The symbol representing the traded instrument.</param>
    public LegAttributes(OrderAction action, InstrumentType instrumentType, decimal quantity, string symbol)
    {
        Action = action;
        InstrumentType = instrumentType;
        Quantity = quantity;
        Symbol = symbol;
    }
}
