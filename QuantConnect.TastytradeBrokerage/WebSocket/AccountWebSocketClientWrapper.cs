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

using System;
using System.Timers;
using System.Threading;
using QuantConnect.Brokerages.Tastytrade.Api;
using QuantConnect.Brokerages.Tastytrade.Models.Stream;

namespace QuantConnect.Brokerages.Tastytrade.WebSocket;

public class AccountWebSocketClientWrapper : BaseWebSocketClientWrapper
{
    /// <summary>
    /// Static counter used to generate unique request IDs for outgoing messages.
    /// </summary>
    private static int _current;

    /// <summary>
    /// Gets the next unique request ID in a thread-safe manner.
    /// </summary>
    private static int NextRequestId => Interlocked.Increment(ref _current);

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountWebSocketClientWrapper"/> class.
    /// </summary>
    /// <param name="tastytradeApiClient">The API client used to obtain session tokens for authenticated communication.</param>
    /// <param name="accountUpdatesWsUrl"></param>
    public AccountWebSocketClientWrapper(TastytradeApiClient tastytradeApiClient, string accountUpdatesWsUrl, EventHandler<WebSocketMessage> accountUpdateMessageHandler)
        : base(tastytradeApiClient)
    {
        Initialize(accountUpdatesWsUrl);
        Open += SendConnectMessage;
        Message += accountUpdateMessageHandler;
    }

    /// <summary>
    /// Sends a heartbeat message to the server when the timer elapses.
    /// This helps keep the WebSocket connection alive.
    /// </summary>
    /// <param name="sender">The timer instance that triggered the event.</param>
    /// <param name="e">Event data associated with the timer's elapsed event.</param>
    protected override void SendMessageByTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if (!IsOpen)
        {
            return;
        }

        var (tokenType, sessionToken) = _tastyTradeApiClient.TokenProvider.GetAccessToken(default);
        Send(new HeartbeatRequest(tokenType, sessionToken, NextRequestId).ToJson());
    }

    /// <summary>
    /// Subscribes to WebSocket notifications when an external event is triggered. 
    /// Establishes a connection using the session token and account number from the TastyTrade API client.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void SendConnectMessage(object sender, EventArgs e)
    {
        var (tokenType, sessionToken) = _tastyTradeApiClient.TokenProvider.GetAccessToken(default);
        var accountNumber = _tastyTradeApiClient.AccountNumber;

        Send(new ConnectRequest(tokenType, sessionToken, NextRequestId, accountNumber).ToJson());
    }
}
