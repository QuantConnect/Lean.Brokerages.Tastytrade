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

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;

/// <summary>
/// Represents the base class for subscription messages sent to a streaming server.
/// </summary>
public abstract class BaseSubscribeMessage
{
    /// <summary>
    /// The type of action being performed (e.g., "heartbeat", "connect", etc.).
    /// </summary>
    public abstract ActionStream Action { get; }

    /// <summary>
    /// Optional account numbers to subscribe to.
    /// </summary>
    [JsonProperty("value")]
    public string[] AccountNumbers { get; } = null;

    /// <summary>
    /// The session token used to authenticate the request.
    /// Should be the <c>session-token</c> returned during session creation.
    /// </summary>
    public string AuthToken { get; }

    /// <summary>
    /// Optional identifier to correlate the request and response.
    /// Not required, but servers will echo it back in the response.
    /// </summary>
    public int RequestId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSubscribeMessage"/> class.
    /// </summary>
    /// <param name="authToken">
    /// The session token used to authenticate the request. This should be the
    /// <c>session-token</c> value returned from the session creation response.
    /// </param>
    /// <param name="requestId">
    /// An optional client-defined identifier to correlate the request and its corresponding response.
    /// </param>
    /// <param name="accountNumber">
    /// An optional account number to subscribe to. If not specified, no account-specific data will be subscribed.
    /// </param>
    protected BaseSubscribeMessage(string authToken, int requestId, string accountNumber = default)
    {
        AccountNumbers = string.IsNullOrEmpty(accountNumber) ? null : [accountNumber];
        AuthToken = authToken;
        RequestId = requestId;
    }

    /// <summary>
    /// Serializes this <see cref="BaseSubscribeMessage"/> instance to a JSON string
    /// using kebab-case naming for properties as defined in <see cref="JsonSettings.KebabCase"/>.
    /// </summary>
    /// <returns>A kebab-case JSON string representing the current message.</returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, JsonSettings.KebabCase);
    }
}
