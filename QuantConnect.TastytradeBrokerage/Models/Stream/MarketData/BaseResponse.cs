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

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

/// <summary>
/// Represents a basic response message received over a WebSocket connection.
/// </summary>
public class BaseResponse
{
    /// <summary>
    /// Gets the type of the event, which is used to determine how to handle the response.
    /// This value is commonly used in a <c>switch</c> statement to route to the appropriate handler.
    /// </summary>
    public EventType Type { get; }

    /// <summary>
    /// Gets the communication channel on which the message was received.
    /// </summary>
    public int Channel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseResponse"/> struct.
    /// </summary>
    /// <param name="type">The type of the event, used for message dispatching logic.</param>
    /// <param name="channel">The channel identifier on which the message was received.</param>
    [JsonConstructor]
    public BaseResponse(EventType type, int channel) => (Type, Channel) = (type, channel);
}
