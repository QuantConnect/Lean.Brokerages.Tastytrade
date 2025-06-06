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
using QuantConnect.Brokerages.Tastytrade.Serialization;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

/// <summary>
/// Represents an authorization message used to initiate authentication over a WebSocket connection.
/// </summary>
public readonly struct AuthorizationRequest
{
    /// <summary>
    /// Gets the type of the event, which is always <see cref="EventType.Authorization"/>.
    /// Used to identify the message type on the receiving end.
    /// </summary>
    public EventType Type => EventType.Authorization;

    /// <summary>
    /// Gets the communication channel number for the message. 
    /// This is always <c>0</c> for authentication messages.
    /// </summary>
    public int Channel => 0;

    /// <summary>
    /// Gets the authentication token to be used for establishing authorization.
    /// </summary>
    public string Token { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationRequest"/> struct with the specified token.
    /// </summary>
    /// <param name="token">The authentication token provided by the client.</param>
    public AuthorizationRequest(string token)
    {
        Token = token;
    }

    /// <summary>
    /// Serializes the current <see cref="AuthorizationRequest"/> message to a JSON string
    /// using camelCase property naming conventions.
    /// </summary>
    /// <returns>A JSON representation of the authorization message.</returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, JsonSettings.CamelCase);
    }
}
