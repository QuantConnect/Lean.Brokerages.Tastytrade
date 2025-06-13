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
using QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream;

/// <summary>
/// Represents the server's response to a <see cref="ConnectRequest"/> message.
/// This response confirms the result of a connection attempt, indicating
/// either a successful subscription to account updates or an error.
/// </summary>
public sealed class ConnectResponse : BaseAccountMaintenanceStatus
{
    /// <summary>
    /// Gets the list of account numbers that are successfully subscribed
    /// for real-time streaming updates. This value may be <c>null</c> or empty
    /// if the connection attempt failed.
    /// </summary>
    [JsonProperty("value")]
    public string[] AccountNumbers { get; set; }

    /// <summary>
    /// Gets a message returned by the server, which may provide additional
    /// context about the success or failure of the connection attempt.
    /// For example, this may contain an error message such as <c>"failed"</c>.
    /// </summary>
    public string Message { get; set; }
}
