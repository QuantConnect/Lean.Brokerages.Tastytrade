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

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;

/// <summary>
/// Represents the base structure of a response message received from the streaming server.
/// This class provides common properties shared across all types of server responses.
/// </summary>
public class BaseAccountMaintenanceStatus
{
    /// <summary>
    /// Gets the status of the response, indicating whether the request was successful or resulted in an error.
    /// </summary>
    public Status Status { get; set; }

    /// <summary>
    /// Gets the type of action this response corresponds to, such as <c>connect</c> or other stream operations.
    /// </summary>
    public ActionStream Action { get; set; }

    /// <summary>
    /// Gets the unique identifier assigned to the current WebSocket session. This value can be used
    /// for tracing or debugging purposes across multiple messages in the same session.
    /// </summary>
    public string WebSocketSessionId { get; set; }

    /// <summary>
    /// Gets the client-defined request identifier used to correlate this response with its originating request.
    /// </summary>
    public int RequestId { get; set; }
}
