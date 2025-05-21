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
/// Represents the server's response to a <see cref="Connect"/> message.
/// This response confirms the result of a connection attempt, indicating
/// either a successful subscription to account updates or an error.
/// </summary>
public sealed class ConnectResponse : BaseResponseMessage
{
    /// <summary>
    /// Gets the list of account numbers that are successfully subscribed
    /// for real-time streaming updates. This value may be <c>null</c> or empty
    /// if the connection attempt failed.
    /// </summary>
    public string[] AccountNumbers { get; }

    /// <summary>
    /// Gets a message returned by the server, which may provide additional
    /// context about the success or failure of the connection attempt.
    /// For example, this may contain an error message such as <c>"failed"</c>.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectResponse"/> class
    /// using the specified response details from the streaming server.
    /// </summary>
    /// <param name="status">
    /// The result status of the connection attempt, such as <c>Success</c> or <c>Error</c>.
    /// </param>
    /// <param name="action">
    /// The action associated with this response, typically <see cref="ActionStream.Connect"/>.
    /// </param>
    /// <param name="webSocketSessionId">
    /// The unique identifier for the WebSocket session, which can be used for session tracking.
    /// </param>
    /// <param name="value">
    /// The list of account numbers successfully subscribed to the stream. This may be <c>null</c> on failure.
    /// </param>
    /// <param name="requestId">
    /// The identifier correlating this response to the original <see cref="Connect"/> request.
    /// </param>
    /// <param name="message">
    /// A message from the server providing additional context about the connection result.
    /// </param>
    [JsonConstructor]
    public ConnectResponse(Status status, ActionStream action, string webSocketSessionId, string[] value, int requestId, string message)
        : base(status, action, webSocketSessionId, requestId)
    {
        AccountNumbers = value;
        Message = message;
    }
}
