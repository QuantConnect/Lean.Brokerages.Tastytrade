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
using QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

namespace QuantConnect.Brokerages.Tastytrade.WebSocket;

/// <summary>
/// WebSocket client wrapper for handling market data connections through the Tastytrade API.
/// Manages token lifecycle and establishes a connection using DxLink URL.
/// </summary>
public class MarketDataWebSocketClientWrapper : BaseWebSocketClientWrapper
{
    /// <summary>
    /// Cached quote streamer token used to authenticate the WebSocket connection.
    /// </summary>
    private string _token;

    /// <summary>
    /// Expiration time for the quote streamer token.
    /// Quote streamer tokens are valid for 24 hours.
    /// </summary>
    private DateTime _tokenExpirationTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketDataWebSocketClientWrapper"/> class.
    /// Automatically subscribes to notifications and initializes the WebSocket using a fresh token and DxLink URL.
    /// </summary>
    /// <param name="tastytradeApiClient">The Tastytrade API client used to retrieve tokens and URLs.</param>
    public MarketDataWebSocketClientWrapper(TastytradeApiClient tastytradeApiClient)
        : base(tastytradeApiClient)
    {
        Open += SubscribeOnNotifications;
        Initialize(GetApiQuoteTokenAndDxLinkUrl().DxLinkUrl);
    }

    /// <summary>
    /// Retrieves a fresh quote token and DxLink URL from the Tastytrade API if the current token is null or expired.
    /// If the token is still valid, returns the cached token and a null DxLink URL.
    /// </summary>
    /// <returns>A tuple containing the token and DxLink URL. DxLink URL is null if the token is not refreshed.</returns>
    private (string Token, string DxLinkUrl) GetApiQuoteTokenAndDxLinkUrl()
    {
        if (string.IsNullOrEmpty(_token) || DateTime.UtcNow >= _tokenExpirationTime)
        {
            var apiQuoteToken = _tastyTradeApiClient.GetApiQuoteToken().SynchronouslyAwaitTaskResult();

            _token = apiQuoteToken.Token;
            // Quote streamer tokens are valid for 24 hours.
            _tokenExpirationTime = DateTime.UtcNow.AddHours(24).AddMinutes(-1); // buffer 1 mins
            Log.Debug($"{nameof(MarketDataWebSocketClientWrapper)}.{nameof(GetApiQuoteTokenAndDxLinkUrl)}: New token received. It will expire at {_tokenExpirationTime:u} (in 23 hours and 59 minutes).");
            return (_token, apiQuoteToken.DxlinkUrl);
        }

        if (Log.DebuggingEnabled)
        {
            Log.Debug($"{nameof(MarketDataWebSocketClientWrapper)}.{nameof(GetApiQuoteTokenAndDxLinkUrl)}: Reusing valid token. Time until expiration: {(_tokenExpirationTime - DateTime.UtcNow):hh\\:mm\\:ss}");
        }

        return (_token, null);
    }

    /// <summary>
    /// Handles the timer's elapsed event by sending a keep-alive message to maintain the WebSocket connection.
    /// </summary>
    /// <param name="_">The source of the timer event.</param>
    /// <param name="__">The event data containing information about the timer interval.</param>
    /// <remarks>
    /// This method is triggered at regular intervals to prevent the connection from timing out. 
    /// It sends a serialized <see cref="KeepAlive"/> message using the <see cref="Send"/> method. 
    /// According to DxLink's protocol, a keep-alive message must be sent at least once every 60 seconds.
    /// </remarks>
    protected override void SendMessageByTimerElapsed(object _, ElapsedEventArgs __)
    {
        if (!IsOpen)
        {
            return;
        }

        Send(new KeepAlive().ToJson());
    }

    /// <summary>
    /// Subscribes to WebSocket notifications when an external event is triggered. 
    /// Establishes a connection using the session token and account number from the TastyTrade API client.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void SubscribeOnNotifications(object sender, EventArgs e)
    {
        var token = GetApiQuoteTokenAndDxLinkUrl().Token;

        Task.Run(() => HandleWebSocketConnection(token));
    }

    /// <summary>
    /// Establishes and authenticates a WebSocket connection by sending a series of setup, authorization,
    /// and configuration messages. Waits for specific events to confirm successful communication steps.
    /// </summary>
    /// <param name="token">The authorization token required for the connection.</param>
    /// <exception cref="TimeoutException">Thrown when a response is not received within the expected timeframe.</exception>
    /// <exception cref="Exception">Thrown when an error message is received from the WebSocket stream.</exception>
    /// <exception cref="NotSupportedException">Thrown when an unexpected message type is received.</exception>
    private void HandleWebSocketConnection(string token)
    {
        using var autoResetEvent = new AutoResetEvent(false);

        void OnMessageReceived(object _, WebSocketMessage e)
        {
            if (e.Data is TextMessage textMessage)
            {
                Log.Debug($"{nameof(MarketDataWebSocketClientWrapper)}.{nameof(HandleWebSocketConnection)}{nameof(OnMessageReceived)}.WebSocketMessage: {textMessage.Message}");

                var connectResponse = textMessage.Message.DeserializeCamelCase<BaseResponse>();

                switch (connectResponse.Type)
                {
                    case EventType.Setup:
                    case EventType.FeedConfig:
                    case EventType.ChannelOpened:
                        autoResetEvent.Set();
                        break;
                    case EventType.AuthorizationState:
                        var authorizationResponse = textMessage.Message.DeserializeCamelCase<AuthorizationResponse>();
                        if (authorizationResponse.State == AuthorizationState.Authorized)
                        {
                            autoResetEvent.Set();
                        }
                        break;
                    case EventType.Error:
                        var errorResponse = textMessage.Message.DeserializeCamelCase<ErrorStreamResponse>();
                        throw new Exception($"{nameof(MarketDataWebSocketClientWrapper)}.{nameof(HandleWebSocketConnection)}.{nameof(OnMessageReceived)}.Error: {errorResponse}");
                    default:
                        throw new NotSupportedException($"{nameof(MarketDataWebSocketClientWrapper)}.{nameof(HandleWebSocketConnection)}.{nameof(OnMessageReceived)}.Response.Message: {textMessage.Message}");
                }
            }
        }

        try
        {
            Message += OnMessageReceived;

            // 1. SETUP
            Send(new SetupConnection().ToJson());
            WaitOrTimeout(autoResetEvent, "SETUP");

            // 2. AUTHORIZE
            Send(new Authorization(token).ToJson());
            WaitOrTimeout(autoResetEvent, "AUTHORIZE");

            // 3. CHANNEL_REQUEST
            Send(new ChannelRequest().ToJson());
            WaitOrTimeout(autoResetEvent, "CHANNEL_REQUEST");

            // 4. FEED_SETUP
            Send(new FeedSetup().ToJson());
            WaitOrTimeout(autoResetEvent, "FEED_SETUP");

            AuthenticatedResetEvent.Set();
        }
        finally
        {
            Message -= OnMessageReceived;
        }
    }

    /// <summary>
    /// Waits for the AutoResetEvent to be signaled within the given timeout in seconds.
    /// Throws TimeoutException with detailed step information if the wait times out.
    /// </summary>
    /// <param name="autoResetEvent">The AutoResetEvent to wait on.</param>
    /// <param name="step">The descriptive step name for the timeout exception message.</param>
    /// <param name="timeoutSeconds">The maximum time to wait in seconds. Default is 5 seconds.</param>
    private static void WaitOrTimeout(AutoResetEvent autoResetEvent, string step, int timeoutSeconds = 5)
    {
        if (!autoResetEvent.WaitOne(TimeSpan.FromSeconds(timeoutSeconds)))
        {
            throw new TimeoutException($"{nameof(MarketDataWebSocketClientWrapper)}.{nameof(HandleWebSocketConnection)} Timeout waiting for {step} response.");
        }
    }
}
