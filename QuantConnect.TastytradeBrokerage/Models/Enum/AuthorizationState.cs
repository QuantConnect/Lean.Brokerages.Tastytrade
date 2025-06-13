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
/// Represents the authorization state of the client as communicated by the server.
/// The server will notify the client with the AUTH_STATE message. 
/// The <c>state</c> field is set to <see cref="Authorized"/> or <see cref="Unauthorized"/>.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AuthorizationState
{
    /// <summary>
    /// The authorization state is unknown. This is the default value.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The client is not authorized.
    /// </summary>
    [EnumMember(Value = "UNAUTHORIZED")]
    Unauthorized = 1,

    /// <summary>
    /// The client is authorized.
    /// </summary>
    [EnumMember(Value = "AUTHORIZED")]
    Authorized = 2,
}
