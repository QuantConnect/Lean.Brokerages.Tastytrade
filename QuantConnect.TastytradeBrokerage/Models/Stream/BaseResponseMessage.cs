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
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream;

/// <summary>
/// Represents the base structure of a response message received from the streaming server.
/// This class provides common properties shared across all types of server responses.
/// </summary>
public class BaseResponseMessage
{
    /// <summary>
    /// Gets the status of the response, indicating whether the request was successful or resulted in an error.
    /// </summary>
    public Status Status { get; }

    /// <summary>
    /// Gets the type of action this response corresponds to, such as <c>connect</c> or other stream operations.
    /// </summary>
    public ActionStream Action { get; }

    /// <summary>
    /// Gets the unique identifier assigned to the current WebSocket session. This value can be used
    /// for tracing or debugging purposes across multiple messages in the same session.
    /// </summary>
    public string WebSocketSessionId { get; }

    /// <summary>
    /// Gets the client-defined request identifier used to correlate this response with its originating request.
    /// </summary>
    public int RequestId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseResponseMessage"/> class with the
    /// specified response metadata.
    /// </summary>
    /// <param name="status">
    /// The status indicating whether the associated request was processed successfully or not.
    /// </param>
    /// <param name="action">
    /// The action associated with this response message.
    /// </param>
    /// <param name="webSocketSessionId">
    /// The unique session ID assigned by the server for this WebSocket connection.
    /// </param>
    /// <param name="requestId">
    /// The request ID used to match the response with its originating client request.
    /// </param>
    [JsonConstructor]
    public BaseResponseMessage(Status status, ActionStream action, string webSocketSessionId, int requestId)
    {
        Status = status;
        Action = action;
        WebSocketSessionId = webSocketSessionId;
        RequestId = requestId;
    }
}
