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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Logging;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Api;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

namespace QuantConnect.Brokerages.Tastytrade.WebSocket;

public abstract class DualWebSocketsBrokerage : Brokerage
{
    private const int ConnectionTimeout = 30000;

    protected bool IsAccountWebSocketInitialized { get; private set; }
    protected bool IsMarketWebSocketInitialized { get; private set; }

    protected BaseWebSocketClientWrapper AccountUpdatesWebSocket { get; private set; }
    protected BaseWebSocketClientWrapper MarketDataUpdatesWebSocket { get; private set; }

    /// <summary>
    /// Count subscribers for each (symbol, tickType) combination
    /// </summary>
    protected DataQueueHandlerSubscriptionManager SubscriptionManager { get; set; }

    /// <summary>
    /// Initialize only the account updates WebSocket
    /// </summary>
    protected void InitializeAccountUpdates(string accountUpdatesWsUrl, TastytradeApiClient tastytradeApiClient)
    {
        if (IsAccountWebSocketInitialized)
        {
            return;
        }

        AccountUpdatesWebSocket = new AccountWebSocketClientWrapper(tastytradeApiClient, accountUpdatesWsUrl);
        AccountUpdatesWebSocket.Message += OnMessage;

        IsAccountWebSocketInitialized = true;
    }

    /// <summary>
    /// Initialize only the market data updates WebSocket
    /// </summary>
    protected void InitializeMarketDataUpdates(TastytradeApiClient tastytradeApiClient)
    {
        if (IsMarketWebSocketInitialized)
        {
            return;
        }

        MarketDataUpdatesWebSocket = new MarketDataWebSocketClientWrapper(tastytradeApiClient);
        MarketDataUpdatesWebSocket.Message += OnMarketDataMessage;

        //MarketDataUpdatesWebSocket.Open += (sender, args) =>
        //{
        //    Log.Trace($"{nameof(DualWebSocketsBrokerage)}: WebSocket.Open. Subscribing");
        //    Subscribe(GetSubscribed());
        //};

        IsMarketWebSocketInitialized = true;
    }

    /// <summary>
    /// Initialize both WebSockets together
    /// </summary>
    protected void Initialize(string accountUpdatesWsUrl, TastytradeApiClient tastytradeApiClient)
    {
        InitializeAccountUpdates(accountUpdatesWsUrl, tastytradeApiClient);
        InitializeMarketDataUpdates(tastytradeApiClient);
    }

    /// <summary>
    /// Creates an instance of a websockets brokerage
    /// </summary>
    /// <param name="name">Name of brokerage</param>
    protected DualWebSocketsBrokerage(string name) : base(name)
    {
    }

    /// <summary>
    /// Handles websocket received messages
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected abstract void OnMessage(object sender, WebSocketMessage e);

    protected abstract void OnTradeReceived(TradeContent trade);
    protected abstract void OnQuoteReceived(QuoteContent quote);

    protected virtual void OnMarketDataMessage(object _, WebSocketMessage webSocketMessage)
    {
        if (webSocketMessage.Data is WebSocketClientWrapper.TextMessage textMessage)
        {
            var connectResponse = textMessage.Message.DeserializeCamelCase<BaseResponse>();
            switch (connectResponse.Type)
            {
                case EventType.FeedData:
                    var feedData = textMessage.Message.DeserializeCamelCase<FeedData>();
                    switch (feedData.Data.EventType)
                    {
                        case MarketDataEvent.Trade:
                            foreach (var trade in feedData.Data.Content.Cast<TradeContent>())
                            {
                                OnTradeReceived(trade);
                            }
                            break;
                        case MarketDataEvent.Quote:
                            foreach (var quote in feedData.Data.Content.Cast<QuoteContent>())
                            {
                                OnQuoteReceived(quote);
                            }
                            break;
                    }
                    break;
                case EventType.Setup:
                case EventType.FeedConfig:
                case EventType.ChannelOpened:
                case EventType.AuthorizationState:
                    break;
                case EventType.Error:
                    var errorResponse = textMessage.Message.DeserializeCamelCase<ErrorStreamResponse>();
                    throw new Exception($"{nameof(DualWebSocketsBrokerage)}.{nameof(OnMarketDataMessage)}.Error: {errorResponse}");
                default:
                    throw new NotSupportedException($"{nameof(DualWebSocketsBrokerage)}.{nameof(OnMarketDataMessage)}.Response.Message: {textMessage.Message}");
            }
        }
        throw new NotSupportedException();
    }

    /// <summary>
    /// Creates wss connection, monitors for disconnection and re-connects when necessary
    /// </summary>
    public override void Connect()
    {
        if (IsConnected)
            return;

        Log.Trace($"{nameof(DualWebSocketsBrokerage)}.Connect(): Connecting...");

        ConnectSync();
    }

    /// <summary>
    /// Handles the creation of websocket subscriptions
    /// </summary>
    /// <param name="symbols"></param>
    protected abstract bool Subscribe(IEnumerable<Symbol> symbols);

    /// <summary>
    /// Gets a list of current subscriptions
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<Symbol> GetSubscribed()
    {
        return SubscriptionManager?.GetSubscribedSymbols() ?? Enumerable.Empty<Symbol>();
    }

    /// <summary>
    /// Connects all initialized WebSockets synchronously and waits for confirmation.
    /// Throws a <see cref="TimeoutException"/> if a connection is not established within the timeout period.
    /// </summary>
    /// <exception cref="TimeoutException">Thrown when a WebSocket does not connect within the allotted timeout.</exception>
    protected void ConnectSync()
    {
        var webSockets = new (string Name, BaseWebSocketClientWrapper Socket)[]
        {
            ("AccountUpdatesWebSocket", AccountUpdatesWebSocket),
            ("MarketDataWebSocket", MarketDataUpdatesWebSocket)
        };

        foreach (var (webSocketName, webSocket) in webSockets)
        {
            if (webSocket == null)
            {
                Log.Trace($"{nameof(DualWebSocketsBrokerage)}.{nameof(ConnectSync)}.{webSocketName}: Skipping null WebSocket instance.");
                continue;
            }

            webSocket.Connect();

            if (!webSocket.AuthenticatedResetEvent.WaitOne(ConnectionTimeout))
            {
                throw new TimeoutException($"{nameof(DualWebSocketsBrokerage)}.{nameof(ConnectSync)}.{webSocketName}: failed to connect within {ConnectionTimeout}ms.");
            }
        }
    }
}
