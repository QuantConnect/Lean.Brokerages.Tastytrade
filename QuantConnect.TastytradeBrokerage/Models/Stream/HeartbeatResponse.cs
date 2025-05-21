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

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream;

/// <summary>
/// Represents a response from the server indicating that the connection is still active.
/// This message is typically sent at regular intervals as a heartbeat to maintain the WebSocket session.
/// </summary>
public sealed class HeartbeatResponse : BaseResponseMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatResponse"/> class using the specified response metadata.
    /// </summary>
    /// <param name="status">
    /// The status of the heartbeat response, usually indicating success if the connection is healthy.
    /// </param>
    /// <param name="action">
    /// The action type for this response, typically <see cref="ActionStream.Heartbeat"/>.
    /// </param>
    /// <param name="webSocketSessionId">
    /// The unique identifier of the active WebSocket session.
    /// </param>
    /// <param name="requestId">
    /// The identifier used to correlate this heartbeat response with the original request, if applicable.
    /// </param>
    public HeartbeatResponse(Status status, ActionStream action, string webSocketSessionId, int requestId)
        : base(status, action, webSocketSessionId, requestId)
    {
    }
}
