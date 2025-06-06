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

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

/// <summary>
/// Represents a symbol and the type of data associated with it (e.g., Trade or Quote).
/// </summary>
public readonly struct SymbolType
{
    /// <summary>
    /// Gets the symbol name.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the type of data for the symbol (e.g., "Trade", "Quote").
    /// </summary>
    public MarketDataEvent Type { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolType"/> struct.
    /// </summary>
    /// <param name="symbol">The symbol name.</param>
    /// <param name="type">The type of data.</param>
    public SymbolType(string symbol, MarketDataEvent type) => (Symbol, Type) = (symbol, type);
}