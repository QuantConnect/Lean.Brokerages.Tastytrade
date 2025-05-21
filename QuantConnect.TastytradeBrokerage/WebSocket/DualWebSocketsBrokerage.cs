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
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Logging;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Api;
namespace QuantConnect.Brokerages.Tastytrade.WebSocket;

public abstract class DualWebSocketsBrokerage : Brokerage
{
    private const int ConnectionTimeout = 30000;

    /// <summary>
    /// True if the current brokerage is already initialized
    /// </summary>
    protected bool IsInitialized { get; set; }

    protected IWebSocket AccountUpdatesWebSocket { get; set; }

    protected IWebSocket MarketDataUpdatesWebSocket { get; set; }

    /// <summary>
    /// Count subscribers for each (symbol, tickType) combination
    /// </summary>
    protected DataQueueHandlerSubscriptionManager SubscriptionManager { get; set; }

    /// <summary>
    /// Initialize the instance of this class
    /// </summary>
    protected void Initialize(string accountUpdatesWsUrl, TastytradeApiClient tastytradeApiClient)
    {
        // TODO: think how can I creating instances AccountUpdates and MarketUpdates independently when Lean will be use it only like Brokerage or DQHб probablly, should create 2 different Initialize methods
        if (IsInitialized)
        {
            return;
        }
        IsInitialized = true;

        AccountUpdatesWebSocket = new AccountWebSocketClientWrapper(tastytradeApiClient);
        AccountUpdatesWebSocket.Initialize(accountUpdatesWsUrl);
        AccountUpdatesWebSocket.Message += OnMessage;

        //MarketDataUpdatesWebSocket = new MarketDataWebSocketClientWrapper();
        //MarketDataUpdatesWebSocket.Open += (sender, args) =>
        //{
        //    Log.Trace($"{nameof(DualWebsocketsBrokerage)}: WebSocket.Open. Subscribing");
        //    Subscribe(GetSubscribed());
        //};
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
    /// Start websocket connect
    /// </summary>
    protected void ConnectSync()
    {
        var resetEvent = new ManualResetEvent(false);
        EventHandler triggerEvent = (o, args) => resetEvent.Set();
        AccountUpdatesWebSocket.Open += triggerEvent;

        AccountUpdatesWebSocket.Connect();

        if (!resetEvent.WaitOne(ConnectionTimeout))
        {
            throw new TimeoutException("Websockets connection timeout.");
        }
        AccountUpdatesWebSocket.Open -= triggerEvent;
    }
}
