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
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using QuantConnect.Brokerages.Tastytrade.Api;
using QuantConnect.Brokerages.Tastytrade.WebSocket;

namespace QuantConnect.Brokerages.Tastytrade;

/// <summary>
/// Represents the Tastytrade Brokerage implementation.
/// </summary>
[BrokerageFactory(typeof(TastytradeBrokerageFactory))]
public partial class TastytradeBrokerage : DualWebSocketsBrokerage
{
    /// <summary>
    /// Represents the name of the market or broker being used, in this case, "Tastytrade".
    /// </summary>
    private static readonly string BrokerageName = "Tastytrade";

    private EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;

    /// <summary>
    /// The Tastytrade api client implementation.
    /// </summary>
    private TastytradeApiClient _tastytradeApiClient;

    /// <summary>
    /// Provides the mapping between Lean symbols and brokerage specific symbols.
    /// </summary>
    private TastytradeBrokerageSymbolMapper _symbolMapper;

    /// <summary>
    /// Returns true if we're currently connected to the broker
    /// </summary>
    public override bool IsConnected { get; }

    /// <summary>
    /// Parameterless constructor for brokerage
    /// </summary>
    /// <remarks>This parameterless constructor is required for brokerages implementing <see cref="IDataQueueHandler"/></remarks>
    public TastytradeBrokerage() : base(BrokerageName)
    { }

    public TastytradeBrokerage(string baseUrl, string baseWSUrl, string username, string password, string accountNumber, IAlgorithm algorithm) : base(BrokerageName)
    {
        Initialize(baseUrl, baseWSUrl, username, password, accountNumber, algorithm);
    }


    protected void Initialize(string baseUrl, string baseWSUrl, string username, string password, string accountNumber, IAlgorithm algorithm)
    {
        _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
        _subscriptionManager.SubscribeImpl += (s, t) => Subscribe(s);
        _subscriptionManager.UnsubscribeImpl += (s, t) => Unsubscribe(s);

        _securityProvider = algorithm?.Portfolio;
        _tastytradeApiClient = new(baseUrl, username, password, accountNumber);
        _symbolMapper = new(_tastytradeApiClient);

        _aggregator = Composer.Instance.GetPart<IDataAggregator>();
        if (_aggregator == null)
        {
            var aggregatorName = Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager");
            Log.Trace($"{nameof(TastytradeBrokerage)}.{nameof(Initialize)}: found no data aggregator instance, creating {aggregatorName}");
            _aggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(aggregatorName);
        }

        Initialize(baseWSUrl, _tastytradeApiClient);

        // Useful for some brokerages:

        // Brokerage helper class to lock websocket message stream while executing an action, for example placing an order
        // avoid race condition with placing an order and getting filled events before finished placing
        // _messageHandler = new BrokerageConcurrentMessageHandler<>();

        // Rate gate limiter useful for API/WS calls
        // _connectionRateLimiter = new RateGate();
    }

    /// <summary>
    /// Connects the client to the broker's remote servers
    /// </summary>
    public override void Connect()
    {
        base.Connect();
    }

    /// <summary>
    /// Disconnects the client from the broker's remote servers
    /// </summary>
    public override void Disconnect()
    {
        throw new NotImplementedException();
    }

    private bool CanSubscribe(Symbol symbol)
    {
        if (symbol.Value.IndexOfInvariant("universe", true) != -1 || symbol.IsCanonical())
        {
            return false;
        }

        throw new NotImplementedException();
    }

    protected override void OnMessage(object sender, WebSocketMessage e)
    {
        throw new NotImplementedException();
    }
}