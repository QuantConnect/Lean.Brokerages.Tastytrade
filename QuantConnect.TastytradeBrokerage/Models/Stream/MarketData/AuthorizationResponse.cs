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

using System.Text.Json.Serialization;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

/// <summary>
/// Represents a response message from the server indicating the result of an authentication attempt over a WebSocket connection.
/// </summary>
public readonly struct AuthorizationResponse
{
    /// <summary>
    /// Gets the type of the event. This is typically <see cref="EventType.AuthorizationState"/> for authentication responses.
    /// </summary>
    public EventType Type { get; }

    /// <summary>
    /// Gets the communication channel on which the message was received.
    /// </summary>
    public int Channel { get; }

    /// <summary>
    /// Gets the authorization state indicating whether the client was successfully authenticated.
    /// </summary>
    public AuthorizationState State { get; }

    /// <summary>
    /// Gets the identifier of the authenticated user, if available.
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationResponse"/> struct.
    /// </summary>
    /// <param name="type">The event type of the message, usually <see cref="EventType.AuthorizationState"/>.</param>
    /// <param name="channel">The communication channel used for this message.</param>
    /// <param name="state">The authorization state (e.g., Authorized or Unauthorized).</param>
    /// <param name="userId">The user ID associated with the authenticated session, if applicable.</param>
    [JsonConstructor]
    public AuthorizationResponse(EventType type, int channel, AuthorizationState state, string userId)
    {
        Type = type;
        Channel = channel;
        State = state;
        UserId = userId;
    }
}
