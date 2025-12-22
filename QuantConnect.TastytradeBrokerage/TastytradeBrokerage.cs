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
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Api;
using QuantConnect.Util;
using QuantConnect.Data;
using QuantConnect.Logging;
using Newtonsoft.Json.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using QuantConnect.Brokerages.Tastytrade.Api;
using QuantConnect.Brokerages.Tastytrade.WebSocket;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Orders;
using QuantConnect.Brokerages.LevelOneOrderBook;
using System.Net.Http;

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
    /// Represents the Lean API connection.
    /// </summary>
    private static ApiConnection _leanApiClient;

    /// <summary>
    /// Provide data from external Lean algorithm.
    /// </summary>
    protected IAlgorithm _algorithm;

    /// <summary>
    /// Returns true if we're currently connected to the broker
    /// </summary>
    public override bool IsConnected =>
        _clientWrapperByWebSocketType[WebSocketType.Account]?.IsOpen == true
        && _clientWrapperByWebSocketType[WebSocketType.MarketData]?.IsOpen == true;

    /// <summary>
    /// Parameterless constructor for brokerage.
    /// </summary>
    /// <remarks>
    /// This parameterless constructor is required for brokerages implementing <see cref="IDataQueueHandler"/>.
    /// </remarks>
    public TastytradeBrokerage() : base(BrokerageName)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TastytradeBrokerage"/> class using token-based authentication.
    /// </summary>
    /// <param name="baseUrl">The base URL for Tastytrade API requests.</param>
    /// <param name="baseWSUrl">The base WebSocket URL for real-time data streams.</param>
    /// <param name="accountNumber">The brokerage account number.</param>
    /// <param name="refreshToken">The refresh token used to obtain new access tokens.</param>
    /// <param name="orderProvider">The order provider responsible for order management.</param>
    /// <param name="securityProvider">The security provider supplying security information.</param>
    /// <param name="algorithm">The algorithm instance interacting with the brokerage.</param>
    public TastytradeBrokerage(string baseUrl, string baseWSUrl, string accountNumber, string refreshToken, IOrderProvider orderProvider, ISecurityProvider securityProvider, IAlgorithm algorithm)
        : this(baseUrl, baseWSUrl, default, default, accountNumber, refreshToken, orderProvider, securityProvider, algorithm)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TastytradeBrokerage"/> class using token-based authentication.
    /// </summary>
    /// <param name="baseUrl">The base URL for Tastytrade API requests.</param>
    /// <param name="baseWSUrl">The base WebSocket URL for real-time data streams.</param>
    /// <param name="accountNumber">The brokerage account number.</param>
    /// <param name="refreshToken">The refresh token used to obtain new access tokens.</param>
    /// <param name="algorithm">The algorithm instance interacting with the brokerage.</param>
    public TastytradeBrokerage(string baseUrl, string baseWSUrl, string accountNumber, string refreshToken, IAlgorithm algorithm)
    : this(baseUrl, baseWSUrl, accountNumber, refreshToken, algorithm?.Portfolio?.Transactions, algorithm?.Portfolio, algorithm)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TastytradeBrokerage"/> class using username and password authentication.
    /// </summary>
    /// <param name="baseUrl">The base URL for Tastytrade API requests.</param>
    /// <param name="baseWSUrl">The base WebSocket URL for real-time data streams.</param>
    /// <param name="username">The username for Tastytrade account authentication.</param>
    /// <param name="password">The password for Tastytrade account authentication.</param>
    /// <param name="accountNumber">The brokerage account number.</param>
    /// <param name="orderProvider">The order provider responsible for order management.</param>
    /// <param name="securityProvider">The security provider supplying security information.</param>
    /// <param name="algorithm">The algorithm instance interacting with the brokerage.</param>
    public TastytradeBrokerage(string baseUrl, string baseWSUrl, string username, string password, string accountNumber, IOrderProvider orderProvider, ISecurityProvider securityProvider, IAlgorithm algorithm)
        : this(baseUrl, baseWSUrl, username, password, accountNumber, default, orderProvider, securityProvider, algorithm)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TastytradeBrokerage"/> class using username and password authentication, providing only algorithm dependencies.
    /// </summary>
    /// <param name="baseUrl">The base URL for Tastytrade API requests.</param>
    /// <param name="baseWSUrl">The base WebSocket URL for real-time data streams.</param>
    /// <param name="username">The username for Tastytrade account authentication.</param>
    /// <param name="password">The password for Tastytrade account authentication.</param>
    /// <param name="accountNumber">The brokerage account number.</param>
    /// <param name="algorithm">The algorithm instance interacting with the brokerage, used to infer order and security providers.</param>
    public TastytradeBrokerage(string baseUrl, string baseWSUrl, string username, string password, string accountNumber, IAlgorithm algorithm)
        : this(baseUrl, baseWSUrl, username, password, accountNumber, default, algorithm?.Portfolio?.Transactions, algorithm?.Portfolio, algorithm)
    { }

    /// <summary>
    /// Central constructor that initializes the <see cref="TastytradeBrokerage"/> with full configuration.
    /// </summary>
    /// <param name="baseUrl">The base URL for Tastytrade API requests.</param>
    /// <param name="baseWSUrl">The base WebSocket URL for real-time data streams.</param>
    /// <param name="username">The username for Tastytrade account authentication (can be empty if using token-based auth).</param>
    /// <param name="password">The password for Tastytrade account authentication (can be empty if using token-based auth).</param>
    /// <param name="accountNumber">The brokerage account number.</param>
    /// <param name="refreshToken">The refresh token for access renewal (optional).</param>
    /// <param name="orderProvider">The order provider responsible for order management.</param>
    /// <param name="securityProvider">The security provider supplying security information.</param>
    /// <param name="algorithm">The algorithm instance interacting with the brokerage.</param>
    private TastytradeBrokerage(string baseUrl, string baseWSUrl, string username, string password, string accountNumber, string refreshToken, IOrderProvider orderProvider, ISecurityProvider securityProvider, IAlgorithm algorithm)
        : base(BrokerageName)
    {
        Initialize(baseUrl, baseWSUrl, username, password, accountNumber, refreshToken, orderProvider, securityProvider, algorithm);
    }

    /// <summary>
    /// Initializes the <see cref="TastytradeBrokerage"/> instance with API credentials, connection settings, and dependencies.
    /// </summary>
    /// <param name="baseUrl">The base URL for Tastytrade REST API requests.</param>
    /// <param name="baseWSUrl">The base WebSocket URL for real-time market data and account events.</param>
    /// <param name="username">The username for Tastytrade account authentication. Leave empty when using token-based authentication.</param>
    /// <param name="password">The password for Tastytrade account authentication. Leave empty when using token-based authentication.</param>
    /// <param name="accountNumber">The brokerage account number to associate with this instance.</param>
    /// <param name="refreshToken">The refresh token used to obtain new access tokens automatically. Optional if using username/password authentication only.</param>
    /// <param name="orderProvider">The order provider responsible for order management and tracking within the algorithm.</param>
    /// <param name="securityProvider">The security provider supplying security definitions and holdings for the algorithm.</param>
    /// <param name="algorithm">The algorithm instance integrating with the brokerage, providing access to portfolio, transactions, and runtime context.</param>
    protected void Initialize(string baseUrl, string baseWSUrl, string username, string password, string accountNumber, string refreshToken, IOrderProvider orderProvider, ISecurityProvider securityProvider, IAlgorithm algorithm)
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;
        ValidateSubscription();

        _algorithm = algorithm;
        _securityProvider = securityProvider;

        if (!string.IsNullOrEmpty(refreshToken))
        {
            Log.Debug($"{nameof(TastytradeBrokerage)}.{nameof(Initialize)}: Using Lean initialization process");
            _tastytradeApiClient = new(baseUrl, Name, accountNumber, refreshToken, _leanApiClient);
        }
        else
        {
            Log.Debug($"{nameof(TastytradeBrokerage)}.{nameof(Initialize)}: Using development initialization process");
            _tastytradeApiClient = new(baseUrl, username, password, accountNumber);
        }

        _symbolMapper = new(_tastytradeApiClient);
        _orderProvider = orderProvider;

        _aggregator = Composer.Instance.GetPart<IDataAggregator>();
        if (_aggregator == null)
        {
            var aggregatorName = Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager");
            Log.Trace($"{nameof(TastytradeBrokerage)}.{nameof(Initialize)}: found no data aggregator instance, creating {aggregatorName}");
            _aggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(aggregatorName);
        }

        _levelOneServiceManager = new LevelOneServiceManager(
        _aggregator,
        (symbols, _) => Subscribe(symbols),
        (symbols, _) => Unsubscribe(symbols));

        // If we are used as a 'data-queue-handler' ignore account updates
        if (!string.IsNullOrEmpty(accountNumber))
        {
            _clientWrapperByWebSocketType[WebSocketType.Account] = new AccountWebSocketClientWrapper(_tastytradeApiClient, baseWSUrl, OnAccountUpdateMessageHandler);
        }
        _clientWrapperByWebSocketType[WebSocketType.MarketData] = new MarketDataWebSocketClientWrapper(_tastytradeApiClient, OnReSubscriptionProcess, OnMarketDataMessageHandler, OnMessage);

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
        return
            symbol.Value.IndexOfInvariant("universe", true) == -1
            && !symbol.IsCanonical()
            && _symbolMapper.SupportedSecurityType.Contains(symbol.SecurityType);
    }

    private class ModulesReadLicenseRead : QuantConnect.Api.RestResponse
    {
        [JsonProperty(PropertyName = "license")]
        public string License;
        [JsonProperty(PropertyName = "organizationId")]
        public string OrganizationId;
    }

    /// <summary>
    /// Validate the user of this project has permission to be using it via our web API.
    /// </summary>
    private static void ValidateSubscription()
    {
        try
        {
            var productId = 412;
            var userId = Globals.UserId;
            var token = Globals.UserToken;
            var organizationId = Globals.OrganizationID;
            // Verify we can authenticate with this user and token
            _leanApiClient = new ApiConnection(userId, token);
            if (!_leanApiClient.Connected)
            {
                throw new ArgumentException("Invalid api user id or token, cannot authenticate subscription.");
            }
            // Compile the information we want to send when validating
            var information = new Dictionary<string, object>()
                {
                    {"productId", productId},
                    {"machineName", Environment.MachineName},
                    {"userName", Environment.UserName},
                    {"domainName", Environment.UserDomainName},
                    {"os", Environment.OSVersion}
                };
            // IP and Mac Address Information
            try
            {
                var interfaceDictionary = new List<Dictionary<string, object>>();
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.OperationalStatus == OperationalStatus.Up))
                {
                    var interfaceInformation = new Dictionary<string, object>();
                    // Get UnicastAddresses
                    var addresses = nic.GetIPProperties().UnicastAddresses
                        .Select(uniAddress => uniAddress.Address)
                        .Where(address => !IPAddress.IsLoopback(address)).Select(x => x.ToString());
                    // If this interface has non-loopback addresses, we will include it
                    if (!addresses.IsNullOrEmpty())
                    {
                        interfaceInformation.Add("unicastAddresses", addresses);
                        // Get MAC address
                        interfaceInformation.Add("MAC", nic.GetPhysicalAddress().ToString());
                        // Add Interface name
                        interfaceInformation.Add("name", nic.Name);
                        // Add these to our dictionary
                        interfaceDictionary.Add(interfaceInformation);
                    }
                }
                information.Add("networkInterfaces", interfaceDictionary);
            }
            catch (Exception)
            {
                // NOP, not necessary to crash if fails to extract and add this information
            }
            // Include our OrganizationId is specified
            if (!string.IsNullOrEmpty(organizationId))
            {
                information.Add("organizationId", organizationId);
            }
            // Create HTTP request
            using var request = ApiUtils.CreateJsonPostRequest("modules/license/read", information);
            _leanApiClient.TryRequest(request, out ModulesReadLicenseRead result);
            if (!result.Success)
            {
                throw new InvalidOperationException($"Request for subscriptions from web failed, Response Errors : {string.Join(',', result.Errors)}");
            }

            var encryptedData = result.License;
            // Decrypt the data we received
            DateTime? expirationDate = null;
            long? stamp = null;
            bool? isValid = null;
            if (encryptedData != null)
            {
                // Fetch the org id from the response if we are null, we need it to generate our validation key
                if (string.IsNullOrEmpty(organizationId))
                {
                    organizationId = result.OrganizationId;
                }
                // Create our combination key
                var password = $"{token}-{organizationId}";
                var key = SHA256.HashData(Encoding.UTF8.GetBytes(password));
                // Split the data
                var info = encryptedData.Split("::");
                var buffer = Convert.FromBase64String(info[0]);
                var iv = Convert.FromBase64String(info[1]);
                // Decrypt our information
                using var aes = new AesManaged();
                var decryptor = aes.CreateDecryptor(key, iv);
                using var memoryStream = new MemoryStream(buffer);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var streamReader = new StreamReader(cryptoStream);
                var decryptedData = streamReader.ReadToEnd();
                if (!decryptedData.IsNullOrEmpty())
                {
                    var jsonInfo = JsonConvert.DeserializeObject<JObject>(decryptedData);
                    expirationDate = jsonInfo["expiration"]?.Value<DateTime>();
                    isValid = jsonInfo["isValid"]?.Value<bool>();
                    stamp = jsonInfo["stamped"]?.Value<int>();
                }
            }
            // Validate our conditions
            if (!expirationDate.HasValue || !isValid.HasValue || !stamp.HasValue)
            {
                throw new InvalidOperationException("Failed to validate subscription.");
            }

            var nowUtc = DateTime.UtcNow;
            var timeSpan = nowUtc - Time.UnixTimeStampToDateTime(stamp.Value);
            if (timeSpan > TimeSpan.FromHours(12))
            {
                throw new InvalidOperationException("Invalid API response.");
            }
            if (!isValid.Value)
            {
                throw new ArgumentException($"Your subscription is not valid, please check your product subscriptions on our website.");
            }
            if (expirationDate < nowUtc)
            {
                throw new ArgumentException($"Your subscription expired {expirationDate}, please renew in order to use this product.");
            }
        }
        catch (Exception e)
        {
            Log.Error($"ValidateSubscription(): Failed during validation, shutting down. Error : {e.Message}");
            Environment.Exit(1);
        }
    }
}