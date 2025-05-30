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
using System.Runtime.Serialization;

namespace QuantConnect.Brokerages.Tastytrade.Models.Enum;

/// <summary>
/// Defines the types of tradable financial instruments supported by the Tastytrade brokerage.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum InstrumentType
{
    /// <summary>
    /// An unknown or unsupported instrument type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A stock or ETF equity instrument.
    /// </summary>
    Equity = 1,

    /// <summary>
    /// An option contract based on an equity instrument.
    /// </summary>
    [EnumMember(Value = "Equity Option")]
    EquityOption = 2,

    /// <summary>
    /// A futures contract.
    /// </summary>
    Future = 3,

    /// <summary>
    /// An option contract based on a futures instrument.
    /// </summary>
    [EnumMember(Value = "Future Option")]
    FutureOption = 4,

    /// <summary>
    /// A cryptocurrency asset (e.g., Bitcoin, Ethereum).
    /// </summary>
    Cryptocurrency = 5
}