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

using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using QuantConnect.Brokerages.Tastytrade.Api;
using QuantConnect.Brokerages.Tastytrade.WebSocket;
using QuantConnect.Brokerages.Tastytrade.Models.Orders;

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
    /// Handles incoming account content messages and processes them using the <see cref="BrokerageConcurrentMessageHandler{T}"/>.
    /// </summary>
    private BrokerageConcurrentMessageHandler<Order> _messageHandler;

    /// <summary>
    /// Order provider
    /// </summary>
    private IOrderProvider _orderProvider;

    /// <summary>
    /// The Tastytrade api client implementation.
    /// </summary>
    private TastytradeApiClient _tastytradeApiClient;

    /// <summary>
    /// Provides the mapping between Lean symbols and brokerage specific symbols.
    /// </summary>
    private protected TastytradeBrokerageSymbolMapper _symbolMapper;

    /// <summary>
    /// Returns true if we're currently connected to the broker
    /// </summary>
    public override bool IsConnected => AccountUpdatesWebSocket?.IsOpen ?? false && IsAccountWebSocketInitialized;

    /// <summary>
    /// Parameterless constructor for brokerage
    /// </summary>
    /// <remarks>This parameterless constructor is required for brokerages implementing <see cref="IDataQueueHandler"/></remarks>
    public TastytradeBrokerage() : base(BrokerageName)
    { }

    public TastytradeBrokerage(string baseUrl, string baseWSUrl, string username, string password, string accountNumber, IOrderProvider orderProvider, ISecurityProvider securityProvider)
    : base(BrokerageName)
    {
        Initialize(baseUrl, baseWSUrl, username, password, accountNumber, orderProvider, securityProvider);
    }

    public TastytradeBrokerage(string baseUrl, string baseWSUrl, string username, string password, string accountNumber, IAlgorithm algorithm)
        : this(baseUrl, baseWSUrl, username, password, accountNumber, algorithm?.Portfolio?.Transactions, algorithm?.Portfolio)
    {

    }

    protected void Initialize(string baseUrl, string baseWSUrl, string username, string password, string accountNumber, IOrderProvider orderProvider, ISecurityProvider securityProvider)
    {
        _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
        _subscriptionManager.SubscribeImpl += (s, t) => Subscribe(s);
        _subscriptionManager.UnsubscribeImpl += (s, t) => Unsubscribe(s);

        _securityProvider = securityProvider;
        _tastytradeApiClient = new(baseUrl, username, password, accountNumber);
        _symbolMapper = new(_tastytradeApiClient);
        _orderProvider = orderProvider;

        _aggregator = Composer.Instance.GetPart<IDataAggregator>();
        if (_aggregator == null)
        {
            var aggregatorName = Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager");
            Log.Trace($"{nameof(TastytradeBrokerage)}.{nameof(Initialize)}: found no data aggregator instance, creating {aggregatorName}");
            _aggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(aggregatorName);
        }

        Initialize(baseWSUrl, _tastytradeApiClient);

        _messageHandler = new BrokerageConcurrentMessageHandler<Order>(OnOrderUpdateReceivedHandler);

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
        base.Disconnect();
    }

    private bool CanSubscribe(Symbol symbol)
    {
        if (symbol.Value.IndexOfInvariant("universe", true) != -1 || symbol.IsCanonical())
        {
            return false;
        }

        return true;
    }
}