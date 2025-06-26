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
using QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream;

/// <summary>
/// Represents a message that initiates a connection to the streaming server
/// for the specified account. This message subscribes the client to receive
/// real-time updates related to the given account.
/// </summary>
public sealed class ConnectRequest : BaseSubscribeMessage
{
    /// <summary>
    /// Gets the action type for this message, which is always <c>"connect"</c>.
    /// </summary>
    public override ActionStream Action => ActionStream.Connect;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectRequest"/> class.
    /// </summary>
    /// <param name="tokenType">The type of the token used for authentication.</param>
    /// <param name="authToken">
    /// The session token used to authenticate the request. This should be the
    /// <c>session-token</c> value returned from the session creation response.
    /// </param>
    /// <param name="requestId">
    /// A client-defined identifier used to correlate the request and its response.
    /// </param>
    /// <param name="accountNumber">
    /// The account number to subscribe to for receiving real-time updates.
    /// </param>
    public ConnectRequest(TokenType tokenType, string authToken, int requestId, string accountNumber)
        : base(tokenType, authToken, requestId, accountNumber)
    {
    }
}
