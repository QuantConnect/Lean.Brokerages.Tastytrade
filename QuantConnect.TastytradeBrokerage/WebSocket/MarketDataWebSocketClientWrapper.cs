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
using QuantConnect.Logging;
using QuantConnect.Brokerages.Tastytrade.Api;
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
    /// Event triggered to initiate the re-subscription process.
    /// </summary>
    private event Action ReSubscriptionProcess;

    /// <summary>
    /// Callback for reporting brokerage-level messages such as errors or data delays.
    /// </summary>
    private readonly Action<BrokerageMessageEvent> _brokerageMessageEvent;

    /// <summary>
    /// Indicates whether a delayed data warning has already been sent to avoid duplicate notifications.
    /// </summary>
    private bool _delayedDataNotified;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketDataWebSocketClientWrapper"/> class.
    /// Automatically subscribes to notifications and initializes the WebSocket using a fresh token and DxLink URL.
    /// </summary>
    /// <param name="tastytradeApiClient">The Tastytrade API client used to retrieve tokens and URLs.</param>
    /// <param name="reSubscriptionHandler">An event handler for re-subscribing to data streams when needed.</param>
    /// <param name="marketDataMessageHandler">The event handler for processing incoming market data messages received from the WebSocket.</param>
    /// <param name="brokerageMessageEvent">A callback to report brokerage-level events, such as connection errors or data delay warnings.</param>
    public MarketDataWebSocketClientWrapper(TastytradeApiClient tastytradeApiClient, Action reSubscriptionHandler, EventHandler<WebSocketMessage> marketDataMessageHandler, Action<BrokerageMessageEvent> brokerageMessageEvent)
        : base(tastytradeApiClient)
    {
        Open += SetupMarketDataConfiguration;
        Message += marketDataMessageHandler;
        ReSubscriptionProcess += reSubscriptionHandler;
        _brokerageMessageEvent = brokerageMessageEvent;
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
            var apiQuoteToken = _tastyTradeApiClient.GetApiQuoteToken();

            _token = apiQuoteToken.Token;
            // Quote streamer tokens are valid for 24 hours.
            _tokenExpirationTime = DateTime.UtcNow.AddHours(24).AddMinutes(-1); // buffer 1 mins
            Log.Debug($"{nameof(MarketDataWebSocketClientWrapper)}.{nameof(GetApiQuoteTokenAndDxLinkUrl)}: New token received. It will expire at {_tokenExpirationTime:u} (in 23 hours and 59 minutes).");

            if (!_delayedDataNotified && apiQuoteToken.DxlinkUrl.Contains("delayed", StringComparison.InvariantCultureIgnoreCase))
            {
                _delayedDataNotified = true;
                _brokerageMessageEvent?.Invoke(new BrokerageMessageEvent(BrokerageMessageType.Warning, "DelayData", $"{nameof(TastytradeBrokerage)}: detected delayed market data in the streaming URL."));
            }

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
    /// It sends a serialized <see cref="KeepAliveRequest"/> message using the <see cref="Send"/> method. 
    /// According to DxLink's protocol, a keep-alive message must be sent at least once every 60 seconds.
    /// </remarks>
    protected override void SendMessageByTimerElapsed(object _, ElapsedEventArgs __)
    {
        if (!IsOpen)
        {
            return;
        }

        Send(new KeepAliveRequest().ToJson());
    }

    /// <summary>
    /// Subscribes to WebSocket notifications when an external event is triggered. 
    /// Establishes a connection using the session token and account number from the TastyTrade API client.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void SetupMarketDataConfiguration(object sender, EventArgs e)
    {
        var token = GetApiQuoteTokenAndDxLinkUrl().Token;

        // 1. SETUP
        Send(new SetupConnectionRequest().ToJson());

        // 2. AUTHORIZE
        Send(new AuthorizationRequest(token).ToJson());

        // 3. CHANNEL_REQUEST
        Send(new ChannelRequest().ToJson());

        // 4. FEED_SETUP
        Send(new FeedSetupRequest().ToJson());

        ReSubscriptionProcess?.Invoke();
    }
}
