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
using QuantConnect.Logging;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Tastytrade.Api;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Stream;

namespace QuantConnect.Brokerages.Tastytrade.WebSocket;

public class AccountWebSocketClientWrapper : BaseWebSocketClientWrapper
{
    private const int ConnectionTimeout = 30000;

    /// <summary>
    /// Static counter used to generate unique request IDs for outgoing messages.
    /// </summary>
    private static int _current;

    /// <summary>
    /// Gets the next unique request ID in a thread-safe manner.
    /// </summary>
    private static int NextRequestId => Interlocked.Increment(ref _current);

    /// <summary>
    /// Provides methods for interacting with the Tastytrade API.
    /// </summary>
    private readonly TastytradeApiClient _tastyTradeApiClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountWebSocketClientWrapper"/> class.
    /// </summary>
    /// <param name="tastyTradeApiClient">The API client used to obtain session tokens for authenticated communication.</param>
    public AccountWebSocketClientWrapper(TastytradeApiClient tastytradeApiClient)
    {
        _tastyTradeApiClient = tastytradeApiClient;
        Open += SubscribeOnNotifications;
    }

    /// <summary>
    /// Sends a heartbeat message to the server when the timer elapses.
    /// This helps keep the WebSocket connection alive.
    /// </summary>
    /// <param name="sender">The timer instance that triggered the event.</param>
    /// <param name="e">Event data associated with the timer's elapsed event.</param>
    protected override void SendMessageByTimerElapsed(object sender, ElapsedEventArgs e)
    {
        var sessionToken = _tastyTradeApiClient.GetSessionToken(default).SynchronouslyAwaitTaskResult();
        Send(new Heartbeat(sessionToken, NextRequestId).ToJson());
    }

    /// <summary>
    /// Subscribes to WebSocket notifications when an external event is triggered. 
    /// Establishes a connection using the session token and account number from the TastyTrade API client.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void SubscribeOnNotifications(object sender, EventArgs e)
    {
        var sessionToken = _tastyTradeApiClient.GetSessionToken(default).SynchronouslyAwaitTaskResult();
        var accountNumber = _tastyTradeApiClient.AccountNumber;

        Task.Run(() => HandleWebSocketConnection(sessionToken, accountNumber));
    }

    /// <summary>
    /// Handles the WebSocket connection process by sending a connect request and awaiting a confirmation message.
    /// Throws an exception if the server denies the connection or if a timeout occurs.
    /// </summary>
    /// <param name="sessionToken">The session token used for authentication.</param>
    /// <param name="accountNumber">The account number for which the connection is made.</param>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the WebSocket connection is denied by the server.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when the connection confirmation is not received within the expected timeout period.
    /// </exception>
    private void HandleWebSocketConnection(string sessionToken, string accountNumber)
    {
        using var autoResetEvent = new AutoResetEvent(false);

        void OnMessageReceived(object _, WebSocketMessage e)
        {
            if (e.Data is TextMessage textMessage)
            {
                Log.Debug($"{nameof(AccountWebSocketClientWrapper)}.{nameof(OnMessageReceived)}.WebSocketMessage: {textMessage.Message}");

                var connectResponse = textMessage.Message.DeserializeKebabCase<ConnectResponse>();

                if (connectResponse.Status == Status.Ok)
                {
                    autoResetEvent.Set();
                }
                else
                {
                    throw new UnauthorizedAccessException(
                        $"WebSocket connection was denied. Server responded with: '{connectResponse.Message}' (Status: {connectResponse.Status})."
                    );
                }
            }
        }

        try
        {
            Message += OnMessageReceived;

            Send(new Connect(sessionToken, NextRequestId, accountNumber).ToJson());

            if (!autoResetEvent.WaitOne(ConnectionTimeout))
            {
                throw new TimeoutException($"{nameof(AccountWebSocketClientWrapper)}.{nameof(HandleWebSocketConnection)}: connection timeout.");
            }
        }
        finally
        {
            Message -= OnMessageReceived;
        }
    }
}
