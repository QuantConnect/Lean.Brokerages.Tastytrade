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
public partial class TastytradeBrokerage : Brokerage
{
    /// <summary>
    /// Represents the name of the market or broker being used, in this case, "Tastytrade".
    /// </summary>
    private static readonly string BrokerageName = "Tastytrade";

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
    /// Indicates whether the initialization process has already been completed.
    /// </summary>
    private bool _isInitialized;

    /// <summary>
    /// Provides the mapping between Lean symbols and brokerage specific symbols.
    /// </summary>
    private protected TastytradeBrokerageSymbolMapper _symbolMapper;

    /// <summary>
    /// Provide data from external Lean algorithm.
    /// </summary>
    protected IAlgorithm _algorithm;

    /// <summary>
    /// Returns true if we're currently connected to the broker
    /// </summary>
    public override bool IsConnected => AccountUpdatesWebSocket?.IsOpen == true || MarketDataUpdatesWebSocket?.IsOpen == true;

    /// <summary>
    /// Parameterless constructor for brokerage
    /// </summary>
    /// <remarks>This parameterless constructor is required for brokerages implementing <see cref="IDataQueueHandler"/></remarks>
    public TastytradeBrokerage() : base(BrokerageName)
    { }

    public TastytradeBrokerage(string baseUrl, string baseWSUrl, string username, string password, string accountNumber, IOrderProvider orderProvider, ISecurityProvider securityProvider, IAlgorithm algorithm)
    : base(BrokerageName)
    {
        Initialize(baseUrl, baseWSUrl, username, password, accountNumber, orderProvider, securityProvider, algorithm);
    }

    public TastytradeBrokerage(string baseUrl, string baseWSUrl, string username, string password, string accountNumber, IAlgorithm algorithm)
        : this(baseUrl, baseWSUrl, username, password, accountNumber, algorithm?.Portfolio?.Transactions, algorithm?.Portfolio, algorithm)
    { }

    protected void Initialize(string baseUrl, string baseWSUrl, string username, string password, string accountNumber, IOrderProvider orderProvider, ISecurityProvider securityProvider, IAlgorithm algorithm)
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;

        SubscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager()
        {
            SubscribeImpl = (s, t) => Subscribe(s),
            UnsubscribeImpl = (s, t) => Unsubscribe(s)
        };

        _algorithm = algorithm;
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

        AccountUpdatesWebSocket = new AccountWebSocketClientWrapper(_tastytradeApiClient, baseWSUrl, OnAccountUpdateMessageHandler);
        MarketDataUpdatesWebSocket = new MarketDataWebSocketClientWrapper(_tastytradeApiClient, OnReSubscriptionProcess, OnMarketDataMessageHandler);

        _messageHandler = new BrokerageConcurrentMessageHandler<Order>(OnOrderUpdateReceivedHandler);
    }

    /// <summary>
    /// Determines whether a given symbol can be subscribed to.
    /// Symbols that are canonical or contain the keyword "universe" (case-insensitive) are excluded.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns><c>true</c> if the symbol is eligible for subscription; otherwise, <c>false</c>.</returns>
    private bool CanSubscribe(Symbol symbol)
    {
        if (symbol.Value.IndexOfInvariant("universe", true) != -1 || symbol.IsCanonical())
        {
            return false;
        }

        return true;
    }
}