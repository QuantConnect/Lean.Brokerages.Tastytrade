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

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

/// <summary>
/// Represents a response message from the server indicating the result of an authentication attempt over a WebSocket connection.
/// </summary>
public struct AuthorizationResponse
{
    /// <summary>
    /// Gets the type of the event. This is typically <see cref="EventType.AuthorizationState"/> for authentication responses.
    /// </summary>
    public EventType Type { get; set; }

    /// <summary>
    /// Gets the communication channel on which the message was received.
    /// </summary>
    public int Channel { get; set; }

    /// <summary>
    /// Gets the authorization state indicating whether the client was successfully authenticated.
    /// </summary>
    public AuthorizationState State { get; set; }

    /// <summary>
    /// Gets the identifier of the authenticated user, if available.
    /// </summary>
    public string UserId { get; set; }
}
