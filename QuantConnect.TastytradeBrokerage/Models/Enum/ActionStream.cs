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
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace QuantConnect.Brokerages.Tastytrade.Models.Enum;

/// <summary>
/// Represents the type of action in a WebSocket message.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ActionStream
{
    /// <summary>
    /// The action is unknown or not recognized.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A message indicating that a connection to the WebSocket has been initiated or confirmed.
    /// </summary>
    [EnumMember(Value = "connect")]
    Connect = 1,

    /// <summary>
    /// A periodic message sent to keep the WebSocket connection alive or to confirm connectivity.
    /// </summary>
    [EnumMember(Value = "heartbeat")]
    Heartbeat = 2
}
