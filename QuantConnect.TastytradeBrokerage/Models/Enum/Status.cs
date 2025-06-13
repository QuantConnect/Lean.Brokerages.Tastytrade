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
/// Indicates the outcome of a WebSocket operation or response.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum Status
{
    /// <summary>
    /// The status is unknown. This is typically used when the status is not provided or cannot be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The WebSocket request was successful and processed without issues.
    /// </summary>
    Ok = 1,

    /// <summary>
    /// The WebSocket request resulted in an error or failure.
    /// </summary>
    Error = 2
}
