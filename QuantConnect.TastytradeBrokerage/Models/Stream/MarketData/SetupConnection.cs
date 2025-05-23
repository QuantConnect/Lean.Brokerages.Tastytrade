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
/// Represents the initial setup message sent by the client to establish a WebSocket connection.
/// This message negotiates protocol version and keep-alive behavior.
/// </summary>
public readonly struct SetupConnection
{
    /// <summary>
    /// Gets the event type for this message, which is always <see cref="EventType.Setup"/>.
    /// </summary>
    public EventType Type => EventType.Setup;

    /// <summary>
    /// Gets the channel identifier for this message. 
    /// Setup and keep-alive messages are always sent on channel <c>0</c>.
    /// </summary>
    public int Channel => 0;

    /// <summary>
    /// Gets the protocol version string used by the client.
    /// This indicates compatibility and expected message formats.
    /// </summary>
    public string Version => "0.1-DXF-JS/0.3.0";

    /// <summary>
    /// Gets the interval (in seconds) at which the client will send keep-alive messages.
    /// </summary>
    public int KeepaliveTimeout => 60;

    /// <summary>
    /// Gets the interval (in seconds) that the client is willing to accept keep-alive messages from the server.
    /// </summary>
    public int AcceptKeepaliveTimeout => 60;

    /// <summary>
    /// Serializes the <see cref="SetupConnection"/> message to a JSON string using camelCase property naming.
    /// </summary>
    /// <returns>A JSON-formatted string representing the setup connection message.</returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, JsonSettings.CamelCase);
    }
}
