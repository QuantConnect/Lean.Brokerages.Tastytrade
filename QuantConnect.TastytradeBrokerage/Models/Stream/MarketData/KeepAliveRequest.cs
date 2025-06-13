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
/// Represents a keep-alive message that must be sent periodically to maintain an open WebSocket connection with DxLink.
/// </summary>
public readonly struct KeepAliveRequest
{
    /// <summary>
    /// Gets the type of this message, which is always <see cref="EventType.KeepAlive"/> for keep-alive messages.
    /// </summary>
    public EventType Type => EventType.KeepAlive;

    /// <summary>
    /// Gets the channel identifier for this message. Keep-alive messages are always sent on channel 0.
    /// </summary>
    public int Channel => 0;

    /// <summary>
    /// Serializes this <see cref="KeepAliveRequest"/> instance to a JSON string
    /// using kebab-case naming for properties as defined in <see cref="JsonSettings.KebabCase"/>.
    /// </summary>
    /// <returns>A JSON string with kebab-case property names representing the keep-alive message.</returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, JsonSettings.CamelCase);
    }
}
