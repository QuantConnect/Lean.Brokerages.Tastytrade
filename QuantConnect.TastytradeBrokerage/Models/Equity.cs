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

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Represents an equity security, indicating whether it is an index (e.g., S&P 500).
/// </summary>
public class Equity : BaseInstrument
{
    /// <summary>
    /// Gets a value indicating whether the equity is an index (e.g., S&P 500).
    /// </summary>
    public bool IsIndex { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Equity"/> class, which represents an equity security
    /// and specifies whether it is an index (e.g., S&P 500).
    /// </summary>
    /// <param name="isIndex">Indicates whether the equity is an index (true) or a regular equity (false).</param>
    /// <param name="symbol">The unique symbol representing the equity.</param>
    /// <param name="streamerSymbol">The symbol used for real-time market data streaming.</param>
    [JsonConstructor]
    public Equity(bool isIndex, string symbol, string streamerSymbol)
        : base(symbol, streamerSymbol)
        => IsIndex = isIndex;
}
