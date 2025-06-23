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
using QuantConnect.Brokerages.LevelOneOrderBook;
using QuantConnect.Brokerages.Tastytrade.WebSocket;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Stream;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.AccountData;

namespace QuantConnect.Brokerages.Tastytrade;

public partial class TastytradeBrokerage
{
    /// <summary>
    /// Stores WebSocket client wrappers indexed by <see cref="WebSocketType"/>.
    /// Used to manage separate WebSocket connections for account and market data updates.
    /// </summary>
    private readonly Dictionary<WebSocketType, BaseWebSocketClientWrapper> _clientWrapperByWebSocketType = new(2)
    {
        { WebSocketType.Account, default },
        { WebSocketType.MarketData, default },
    };

    /// <summary>
    /// Manages Level 1 market data subscriptions and routing of updates to the shared <see cref="IDataAggregator"/>.
    /// Responsible for tracking and updating individual <see cref="LevelOneMarketData"/> instances per symbol.
    /// </summary>
    private LevelOneServiceManager _levelOneServiceManager;

    private const int ConnectionTimeout = 30000;

    /// <summary>
    /// Connects the client to the broker's remote servers
    /// </summary>
    public override void Connect()
    {
        if (IsConnected)
            return;

        Log.Trace($"{nameof(TastytradeBrokerage)}.Connect(): Connecting...");

        foreach (var (webSocketName, webSocket) in _clientWrapperByWebSocketType)
        {
            if (webSocket == null)
            {
                Log.Trace($"{nameof(TastytradeBrokerage)}.{nameof(Connect)}.{webSocketName}: Skipping null WebSocket instance.");
                continue;
            }

            webSocket.Connect();

            if (!webSocket.AuthenticatedResetEvent.WaitOne(ConnectionTimeout))
            {
                throw new TimeoutException($"{nameof(TastytradeBrokerage)}.{nameof(Connect)}.{webSocketName}: failed to connect within {ConnectionTimeout}ms.");
            }
        }
    }

    /// <summary>
    /// Disconnects the client from the broker's remote servers
    /// </summary>
    public override void Disconnect()
    {
        Log.Trace($"{nameof(TastytradeBrokerage)}.Disconnect(): Disconnecting...");

        foreach (var (_, webSocket) in _clientWrapperByWebSocketType)
        {
            if (webSocket?.IsOpen == true)
            {
                webSocket.Close();
            }
        }
    }

    private void OnMarketDataMessageHandler(object _, WebSocketMessage webSocketMessage)
    {
        if (webSocketMessage.Data is WebSocketClientWrapper.TextMessage textMessage)
        {
            if (Log.DebuggingEnabled)
            {
                Log.Debug($"{nameof(TastytradeBrokerage)}.{nameof(OnMarketDataMessageHandler)}.{nameof(MarketDataWebSocketClientWrapper)}.WebSocketMessage: {textMessage.Message}");
            }

            var baseResponse = textMessage.Message.DeserializeCamelCase<BaseMarketDataResponse>();

            switch (baseResponse.Type)
            {
                case EventType.FeedData:
                    var feedData = textMessage.Message.DeserializeCamelCase<FeedData>();
                    var utcNow = DateTime.UtcNow;
                    foreach (var content in feedData.Data.Content)
                    {
                        var leanSymbol = _symbolMapper.GetLeanSymbol(content.Symbol, default);
                        switch (feedData.Data.EventType)
                        {
                            case MarketDataEvent.Trade:
                                OnTradeReceived(content as TradeContent, leanSymbol, utcNow);
                                break;
                            case MarketDataEvent.Quote:
                                OnQuoteReceived(content as QuoteContent, leanSymbol, utcNow);
                                break;
                            case MarketDataEvent.Summary:
                                OnSummaryReceived(content as SummaryContent, leanSymbol, utcNow);
                                break;
                        }
                    }
                    break;
                case EventType.AuthorizationState:
                    var authorizationResponse = textMessage.Message.DeserializeCamelCase<AuthorizationResponse>();
                    if (authorizationResponse.State == AuthorizationState.Authorized)
                    {
                        _clientWrapperByWebSocketType[WebSocketType.MarketData].AuthenticatedResetEvent.Set();
                    }
                    break;
                case EventType.Setup:
                case EventType.FeedConfig:
                case EventType.ChannelOpened:
                case EventType.KeepAlive:
                    break;
                case EventType.Error:
                    var errorResponse = textMessage.Message.DeserializeCamelCase<ErrorStreamResponse>();
                    throw new Exception($"{nameof(TastytradeBrokerage)}.{nameof(OnMarketDataMessageHandler)}.Error: {errorResponse}");
                default:
                    throw new NotSupportedException($"{nameof(TastytradeBrokerage)}.{nameof(OnMarketDataMessageHandler)}.Response.Message: {textMessage.Message}");
            }
        }
        else
        {
            throw new NotSupportedException($"{nameof(TastytradeBrokerage)}.{nameof(OnMarketDataMessageHandler)}: Unsupported WebSocket message type: '{webSocketMessage.Data?.GetType().Name ?? "null"}'.");
        }
    }

    private void OnAccountUpdateMessageHandler(object sender, WebSocketMessage webSocketMessage)
    {
        if (webSocketMessage.Data is WebSocketClientWrapper.TextMessage textMessage)
        {
            if (Log.DebuggingEnabled)
            {
                Log.Debug($"{nameof(TastytradeBrokerage)}.{nameof(OnAccountUpdateMessageHandler)}.{nameof(AccountWebSocketClientWrapper)}.WebSocketMessage: {textMessage.Message}");
            }

            var accountData = textMessage.Message.DeserializeKebabCase<AccountData>();

            switch (accountData.Type)
            {
                case EventType.Order:
                    OnOrderUpdateReceived(accountData.Order);
                    break;
                case EventType.AccountBalance:
                case EventType.CurrentPosition:
                case EventType.TradingStatus:
                    break;
                case EventType.Unknown:
                    var response = textMessage.Message.DeserializeKebabCase<BaseAccountMaintenanceStatus>();
                    switch (response.Action)
                    {
                        case ActionStream.Connect:
                            var connectResponse = textMessage.Message.DeserializeKebabCase<ConnectResponse>();

                            if (connectResponse.Status == Status.Ok)
                            {
                                _clientWrapperByWebSocketType[WebSocketType.Account].AuthenticatedResetEvent.Set();
                            }
                            else
                            {
                                throw new UnauthorizedAccessException(
                                    $"WebSocket connection was denied. Server responded with: '{connectResponse.Message}' (Status: {connectResponse.Status})."
                                );
                            }
                            break;
                        case ActionStream.Heartbeat:
                            if (response.Status != Status.Ok)
                            {
                                throw new InvalidOperationException(
                                    $"{nameof(TastytradeBrokerage)}.{nameof(OnAccountUpdateMessageHandler)}: Received heartbeat with unexpected status '{response.Status}'. Message: {textMessage.Message}");
                            }
                            break;
                        default:
                            throw new NotImplementedException($"{nameof(TastytradeBrokerage)}.{nameof(OnAccountUpdateMessageHandler)}: The action '{response.Action}' in EventType.Unknown is not implemented. Message: {textMessage.Message}");
                    }
                    break;
            }
        }
        else
        {
            throw new NotSupportedException($"{nameof(TastytradeBrokerage)}.{nameof(OnAccountUpdateMessageHandler)}: Unsupported WebSocket message type: '{webSocketMessage.Data?.GetType().Name ?? "null"}'.");
        }
    }

    /// <summary>
    /// Initiates the re-subscription process for available symbols.
    /// If there are no subscriptions available, the process is exited early.
    /// </summary>
    private void OnReSubscriptionProcess()
    {
        Log.Trace($"{nameof(TastytradeBrokerage)}.{nameof(OnReSubscriptionProcess)}: Starting re-subscription process...");

        var subscribedSymbols = _levelOneServiceManager.GetSubscribedSymbols() ?? [];

        if (!subscribedSymbols.Any())
        {
            Log.Trace($"{nameof(TastytradeBrokerage)}.{nameof(OnReSubscriptionProcess)}: No symbols found for re-subscription. Skipping.");
            return;
        }

        // Split into chunks to avoid exceeding WebSocket message size or item count limits.
        foreach (var subscribedSymbolChunk in subscribedSymbols.Chunk(250))
        {
            Subscribe(subscribedSymbolChunk);
        }

        Log.Trace($"{nameof(TastytradeBrokerage)}.{nameof(OnReSubscriptionProcess)}: Re-subscription process completed successfully.");
    }
}
