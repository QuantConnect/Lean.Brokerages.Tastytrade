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
/// Represents an equity security and indicates whether it is an index.
/// </summary>
public readonly struct Equity
{
    /// <summary>
    /// Gets a value indicating whether the equity is an index (e.g., S&P 500).
    /// </summary>
    public bool IsIndex { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Equity"/> struct.
    /// </summary>
    /// <param name="isIndex">A boolean indicating whether the equity is an index.</param>
    [JsonConstructor]
    public Equity(bool isIndex) => IsIndex = isIndex;
}
