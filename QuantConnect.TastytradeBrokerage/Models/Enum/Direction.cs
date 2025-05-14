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
/// Represents the direction of a financial position.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum Direction
{
    /// <summary>
    /// The position is closed or has no exposure (zero position).
    /// </summary>
    Zero = 0,

    /// <summary>
    /// A long position, indicating ownership of the asset with the expectation that its value will rise.
    /// </summary>
    Long = 1,

    /// <summary>
    /// A short position, indicating the sale of an asset not currently owned, typically with the expectation that its value will decline.
    /// </summary>
    Short = 2,
}
