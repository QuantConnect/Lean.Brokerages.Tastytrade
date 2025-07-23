﻿/*
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

using QuantConnect.Brokerages.Authentication;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream;

/// <summary>
/// Represents a heartbeat message sent periodically to the streamer server
/// to prevent the WebSocket connection from being considered stale.
/// </summary>
public sealed class HeartbeatRequest : BaseSubscribeMessage
{
    /// <summary>
    /// Always set to <c>"heartbeat"</c> for heartbeat messages.
    /// </summary>
    public override ActionStream Action => ActionStream.Heartbeat;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatRequest"/> class.
    /// </summary>
    /// <param name="tokenType">The type of the token used for authentication.</param>
    /// <param name="authToken">The session token for authentication.</param>
    /// <param name="requestId">Optional request identifier for tracking.</param>
    public HeartbeatRequest(TokenType tokenType, string authToken, int requestId)
        : base(tokenType, authToken, requestId)
    {
    }
}
