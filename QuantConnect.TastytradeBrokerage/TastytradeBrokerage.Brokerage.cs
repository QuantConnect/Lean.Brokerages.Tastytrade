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
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Orders.Fees;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using QuantConnect.Brokerages.CrossZero;
using LeanOrder = QuantConnect.Orders.Order;
using QuantConnect.Brokerages.Tastytrade.Services;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Orders;
using LeanOrderStatus = QuantConnect.Orders.OrderStatus;
using BrokerageOrder = QuantConnect.Brokerages.Tastytrade.Models.Orders.Order;
using BrokerageOrderStatus = QuantConnect.Brokerages.Tastytrade.Models.Enum.OrderStatus;

[assembly: InternalsVisibleTo("QuantConnect.Brokerages.Tastytrade.Tests")]

namespace QuantConnect.Brokerages.Tastytrade;

/// <summary>
/// Represents the Tastytrade Brokerage implementation.
/// </summary>
public partial class TastytradeBrokerage
{
    /// <summary>
    /// Represents a type capable of fetching the holdings for the specified symbol
    /// </summary>
    private ISecurityProvider _securityProvider;

    /// <summary>
    /// Symbols that Tastytrade doesn't support ("No security definition has been found for the request")
    /// We keep track of them to avoid flooding the logs with the same error/warning
    /// </summary>
    private readonly HashSet<string> _unsupportedSymbols = new();

    /// <summary>
    /// A thread-safe cache that tracks pending orders awaiting confirmation via WebSocket.
    /// </summary>
    /// <remarks>
    /// This cache coordinates order submission and acknowledgment using synchronization primitives.
    /// Each entry is structured as:
    /// <list type="bullet">
    ///   <item>
    ///     <description><b>Key:</b> The brokerage order ID, uniquely identifying the order.</description>
    ///   </item>
    ///   <item>
    ///     <description><b>Value:</b> A <see cref="PendingOrderManager"/> instance containing the order and an <see cref="AutoResetEvent"/> for signaling when a status update is received.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    private readonly ConcurrentDictionary<string, PendingOrderManager> _pendingOrderCache = new();

    /// <summary>
    /// Gets all holdings for the account
    /// </summary>
    /// <returns>The current holdings from the account</returns>
    public override List<Holding> GetAccountHoldings()
    {
        var positions = _tastytradeApiClient.GetAccountPositions();

        var holdings = new List<Holding>();
        foreach (var position in positions)
        {
            if (!TryGetLeanSymbol(position.Symbol, position.InstrumentType, out var leanSymbol, position.UnderlyingSymbol))
            {
                continue;
            }

            var positionQuantity = default(decimal);
            switch (position.QuantityDirection)
            {
                case Direction.Long:
                    positionQuantity = position.Quantity;
                    break;
                case Direction.Short:
                    positionQuantity = decimal.Negate(position.Quantity);
                    break;
                default:
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, 1, $"Unable to determine position direction for symbol '{leanSymbol}'. Full position details: {position}."));
                    continue;
            }

            // TODO: Consider retrieving real-time market prices for each symbol.
            // We can use a batch request to improve performance instead of individual calls.

            holdings.Add(new Holding()
            {
                AveragePrice = position.AverageOpenPrice,
                Quantity = positionQuantity,
                Symbol = leanSymbol
            });
        }

        return holdings;
    }

    /// <summary>
    /// Attempts to map a Tastytrade <paramref name="symbol"/> and <paramref name="instrumentType"/> 
    /// to a Lean-compatible <see cref="Symbol"/>.
    /// </summary>
    /// <param name="symbol">The string representation of the financial instrument's symbol.</param>
    /// <param name="instrumentType">The type of the instrument (e.g., Equity, Option) <see cref="InstrumentType"/>.</param>
    /// <param name="leanSymbol">
    /// The resulting Lean-compatible <see cref="Symbol"/> if the mapping is successful. 
    /// This is an output parameter.
    /// </param>
    /// <param name="optionUnderlyingSymbol">
    /// The underlying symbol for options, used to distinguish between regular options 
    /// and index options. This parameter is optional and defaults to <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the mapping is successful; otherwise, <c>false</c>.
    /// </returns>
    internal bool TryGetLeanSymbol(string symbol, InstrumentType instrumentType, out Symbol leanSymbol, string optionUnderlyingSymbol = null)
    {
        leanSymbol = default;
        try
        {
            leanSymbol = _symbolMapper.GetLeanSymbol(symbol, instrumentType.ConvertInstrumentTypeToSecurityType(), optionUnderlyingSymbol);
            return true;
        }
        catch (Exception ex)
        {
            CheckUnsupportedSymbolError(ex, symbol, instrumentType, "ConvertSymbol");
            return false;
        }
    }

    /// <summary>
    /// Gets the current cash balance for each currency held in the brokerage account
    /// </summary>
    /// <returns>The current cash balance for each currency available for trading</returns>
    public override List<CashAmount> GetCashBalance()
    {
        var balance = _tastytradeApiClient.GetAccountBalances();
        return [new(balance.CashBalance, balance.Currency)];
    }

    /// <summary>
    /// Gets all open orders on the account.
    /// NOTE: The order objects returned do not have QC order IDs.
    /// </summary>
    /// <returns>The open orders returned from IB</returns>
    public override List<LeanOrder> GetOpenOrders()
    {
        var brokerageOrders = _tastytradeApiClient.GetLiveOrders();

        var leanOrders = new List<LeanOrder>();
        foreach (var brokerageOrder in brokerageOrders)
        {
            var orderProperties = new OrderProperties();
            if (!orderProperties.TryGetLeanTimeInForce(brokerageOrder.TimeInForce, brokerageOrder.GtcDate))
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Detected unsupported Lean TimeInForce of '{brokerageOrder.TimeInForce}', ignoring. Using default: TimeInForce.GoodTilCanceled"));
            }

            if (TryCreateLeanOrder(brokerageOrder, orderProperties, out var leanOrder))
            {
                leanOrders.Add(leanOrder);
            }
        }

        return leanOrders;
    }

    /// <summary>
    /// Attempts to create a Lean <see cref="Order"/> instance from a brokerage <see cref="BrokerageOrder"/>.
    /// </summary>
    /// <param name="order">The brokerage order to convert.</param>
    /// <param name="orderProperties">The <see cref="OrderProperties"/> to apply to the Lean order.</param>
    /// <param name="leanOrder">
    /// When this method returns, contains the created Lean <see cref="Order"/> if successful; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the Lean order was successfully created; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Supports market and limit order types only. Emits a warning message for unsupported order types or invalid symbols.
    /// </remarks>
    private bool TryCreateLeanOrder(BrokerageOrder order, OrderProperties orderProperties, out LeanOrder leanOrder)
    {
        leanOrder = default;

        var leg = order.Legs.FirstOrDefault();
        if (!TryGetLeanSymbol(leg.Symbol, leg.InstrumentType, out var leanSymbol, order.UnderlyingSymbol))
        {
            return false;
        }

        var quantity = leg.Action.ToSignedQuantity(leg.Quantity);
        try
        {
            switch (order.OrderType)
            {
                case Models.Enum.OrderType.Market:
                    leanOrder = new MarketOrder(leanSymbol, quantity, order.ReceivedAtUtc, properties: orderProperties);
                    break;
                case Models.Enum.OrderType.Limit:
                    leanOrder = new LimitOrder(leanSymbol, quantity, order.Price, order.ReceivedAtUtc, properties: orderProperties);
                    break;
                case Models.Enum.OrderType.StopLimit:
                    leanOrder = new StopLimitOrder(leanSymbol, quantity, order.StopTrigger, order.Price, order.ReceivedAtUtc, properties: orderProperties);
                    break;
                case Models.Enum.OrderType.Stop:
                    leanOrder = new StopMarketOrder(leanSymbol, quantity, order.StopTrigger, order.ReceivedAtUtc, properties: orderProperties);
                    break;
                default:
                    throw new NotSupportedException($"{nameof(TastytradeBrokerage)}.{nameof(TryCreateLeanOrder)}: The order type '{order.OrderType}' is not supported for conversion to a Lean order.");
            }
        }
        catch (Exception ex)
        {
            CheckUnsupportedSymbolError(ex, leg.Symbol, leg.InstrumentType, "ConvertOrders");
            return false;
        }

        leanOrder.Status = leg.RemainingQuantity != leg.Quantity ? LeanOrderStatus.PartiallyFilled : LeanOrderStatus.Submitted;
        leanOrder.BrokerId.Add(order.Id);

        return true;
    }

    /// <summary>
    /// Places a new order and assigns a new broker ID to the order
    /// </summary>
    /// <param name="order">The order to be placed</param>
    /// <returns>True if the request for a new order has been placed, false otherwise</returns>
    public override bool PlaceOrder(LeanOrder order)
    {
        if (!CanSubscribe(order.Symbol))
        {
            return false;
        }

        var holdingQuantity = _securityProvider.GetHoldingsQuantity(order.Symbol);
        var isPlaceCrossOrder = TryCrossZeroPositionOrder(order, holdingQuantity);

        if (isPlaceCrossOrder == null)
        {
            var orderAction = ResolveOrderAction(order.Direction, order.SecurityType, holdingQuantity);
            PlaceOrderCommon(order, order.AbsoluteQuantity, orderAction);
        }

        return true;
    }

    /// <summary>
    /// Places a cross-zero order and returns a response with the assigned brokerage ID.
    /// </summary>
    /// <param name="crossZeroOrderRequest">The cross-zero order request containing details for execution.</param>
    /// <param name="isPlaceOrderWithLeanEvent">Whether to emit Lean order events during submission.</param>
    /// <returns>
    /// A <see cref="CrossZeroOrderResponse"/> with the assigned brokerage ID, or <c>default</c> if submission failed.
    /// </returns>
    protected override CrossZeroOrderResponse PlaceCrossZeroOrder(CrossZeroFirstOrderRequest crossZeroOrderRequest, bool isPlaceOrderWithLeanEvent = true)
    {
        var orderAction = ResolveOrderAction(crossZeroOrderRequest.LeanOrder.SecurityType, crossZeroOrderRequest.OrderPosition);
        var response = PlaceOrderCommon(crossZeroOrderRequest.LeanOrder, crossZeroOrderRequest.AbsoluteOrderQuantity, orderAction, isPlaceOrderWithLeanEvent);

        return string.IsNullOrEmpty(response) ? default : new CrossZeroOrderResponse(response, true);
    }

    /// <summary>
    /// Core logic for submitting an order to the brokerage and managing pending state.
    /// </summary>
    /// <param name="order">The Lean order to convert and submit.</param>
    /// <param name="orderQuantity">The quantity for the order.</param>
    /// <param name="orderAction">The brokerage-specific order action (buy/sell etc.).</param>
    /// <param name="isInvokeSubmitEvent">Whether to emit a Lean order submitted event.</param>
    /// <returns>The brokerage-assigned order ID, or <c>null</c> if submission failed.</returns>
    private string PlaceOrderCommon(LeanOrder order, decimal orderQuantity, OrderAction orderAction, bool isInvokeSubmitEvent = true)
    {
        var brokerageOrder = ConvertLeanOrderToBrokerageOrder(order, orderQuantity, orderAction);
        var brokerageId = default(string);
        var isPlaced = default(bool);

        var pendingSubmittedOrder = new PendingOrderManager(order, LeanOrderStatus.Submitted)
        {
            IsInvokeOrderEvent = isInvokeSubmitEvent
        };

        _messageHandler.WithLockedStream(() =>
        {
            try
            {
                brokerageId = _tastytradeApiClient.SubmitOrder(brokerageOrder).Order.Id;
            }
            catch (Exception ex)
            {
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "Place Order Event: " + ex.Message)
                {
                    Status = LeanOrderStatus.Invalid
                });
                return;
            }

            order.BrokerId.Add(brokerageId);
            _pendingOrderCache[brokerageId] = pendingSubmittedOrder;
            isPlaced = true;
        });

        if (isPlaced && !pendingSubmittedOrder.AutoResetEvent.WaitOne(TimeSpan.FromSeconds(10)))
        {
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"{nameof(TastytradeBrokerage)}.{nameof(PlaceOrder)}: " +
                $"didn't get response from WebSocket by BrokerageId = {brokerageId} and Lean Order = {order}"));
        }

        pendingSubmittedOrder?.Dispose();

        return brokerageId;
    }

    /// <summary>
    /// Updates the order with the same id
    /// </summary>
    /// <param name="order">The new order information</param>
    /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
    public override bool UpdateOrder(LeanOrder order)
    {
        var brokerageId = order.BrokerId.LastOrDefault();
        var orderAction = ResolveOrderAction(order.Direction, order.SecurityType, _securityProvider.GetHoldingsQuantity(order.Symbol));
        var brokerageOrder = ConvertLeanOrderToBrokerageOrder(order, order.AbsoluteQuantity, orderAction);
        var isUpdatedSuccessfully = true;
        var pendingUpdatedOrder = new PendingOrderManager(order, LeanOrderStatus.UpdateSubmitted);
        var newBrokerageId = default(string);
        _messageHandler.WithLockedStream(() =>
        {
            try
            {
                newBrokerageId = _tastytradeApiClient.ReplaceOrderById(brokerageId, brokerageOrder);

                OnOrderIdChangedEvent(new() { BrokerId = [newBrokerageId], OrderId = order.Id });

                // Placeholder entry to indicate this order is being replaced; avoids invoke order not found message when handle WebSocket messages.
                _pendingOrderCache[brokerageId] = null;
                _pendingOrderCache[newBrokerageId] = pendingUpdatedOrder;
            }
            catch (Exception ex)
            {
                isUpdatedSuccessfully = false;
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, "Update Order: " + ex.Message));
                return;
            }
        });

        if (isUpdatedSuccessfully && !pendingUpdatedOrder.AutoResetEvent.WaitOne(TimeSpan.FromSeconds(100)))
        {
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"{nameof(TastytradeBrokerage)}.{nameof(UpdateOrder)}: " +
                $"didn't get response from WebSocket by BrokerageId = {brokerageId} and Lean Order = {order}"));
        }

        pendingUpdatedOrder?.Dispose();

        return isUpdatedSuccessfully;
    }

    /// <summary>
    /// Cancels the order with the specified ID
    /// </summary>
    /// <param name="order">The order to cancel</param>
    /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
    public override bool CancelOrder(LeanOrder order)
    {
        var canceled = default(bool);
        var brokerageId = order.BrokerId.LastOrDefault();
        _messageHandler.WithLockedStream(() =>
        {
            try
            {
                _tastytradeApiClient.CancelOrderById(brokerageId);
                canceled = true;
            }
            catch (Exception ex)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, "Cancel Order: " + ex.Message));
            }
        });

        return canceled;
    }

    /// <summary>
    /// Entry point for processing order updates received from the external brokerage.
    /// Forwards the update for internal handling and synchronization with Lean's order system.
    /// </summary>
    /// <param name="brokerageOrder">The updated <see cref="BrokerageOrder"/> received from the brokerage.</param>
    private void OnOrderUpdateReceived(BrokerageOrder orderUpdate)
    {
        _messageHandler.HandleNewMessage(orderUpdate);
    }

    /// <summary>
    /// Handles updates to brokerage orders by synchronizing them with their corresponding Lean orders
    /// and emitting appropriate <see cref="OrderEvent"/>s.
    /// </summary>
    /// <param name="orderUpdate">The updated <see cref="BrokerageOrder"/> to process.</param>
    /// <remarks>
    /// Supported <see cref="BrokerageOrderStatus"/> values and their effects:
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="BrokerageOrderStatus.Routed"/> / <see cref="BrokerageOrderStatus.Received"/></term>
    /// <description>
    /// If the associated market is closed, the order is deferred by placing it in the pending submission queue.
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="BrokerageOrderStatus.Live"/></term>
    /// <description>
    /// Immediately marks the order as submitted by processing it from the pending order cache.
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="BrokerageOrderStatus.Filled"/></term>
    /// <description>
    /// Emits a fill <see cref="OrderEvent"/> including price and quantity details, and updates order status to filled.
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="BrokerageOrderStatus.Cancelled"/></term>
    /// <description>
    /// Emits a canceled <see cref="OrderEvent"/>, unless the order is part of an in-progress replacement (e.g., a modification).
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="BrokerageOrderStatus.Expired"/></term>
    /// <description>
    /// Emits a canceled <see cref="OrderEvent"/> due to expiration.
    /// <para><b>Note:</b> An explanatory message for expiration is currently not included (TODO).</para>
    /// </description>
    /// </item>
    /// </list>
    /// If no matching Lean order is found for the update, a warning message is logged.
    /// </remarks>
    private void OnOrderUpdateReceivedHandler(BrokerageOrder orderUpdate)
    {
        switch (orderUpdate.Status)
        {
            case BrokerageOrderStatus.Routed:
            case BrokerageOrderStatus.Live:
                ProcessPendingOrderSubmission(orderUpdate.Id, orderUpdate.ReceivedAtUtc);
                break;
            case BrokerageOrderStatus.Filled:
                var leanOrderStatus = LeanOrderStatus.Filled;
                if (!TryGetLeanOrderByBrokerageId(orderUpdate.Id, leanOrderStatus, out var leanOrder))
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Order not found: {orderUpdate.Id}. Order detail: {orderUpdate}"));
                    break;
                }

                var leg = orderUpdate.Legs.FirstOrDefault();
                var fill = leg.Fills.FirstOrDefault();
                var orderEvent = new OrderEvent(leanOrder, fill.FilledAt, OrderFee.Zero)
                {
                    Status = leanOrderStatus,
                    FillQuantity = leg.Action.ToSignedQuantity(fill.Quantity),
                    FillPrice = fill.FillPrice
                };
                ProcessOrderEventWithCrossZeroCheck(leanOrder, orderEvent);
                break;
            case BrokerageOrderStatus.Cancelled:
                // Skip processing this order because it is part of an update in progress,
                // where the original order ID is being replaced with a new one.
                if (_pendingOrderCache.TryRemove(orderUpdate.Id, out _))
                {
                    break;
                }

                leanOrderStatus = LeanOrderStatus.Canceled;
                if (!TryGetLeanOrderByBrokerageId(orderUpdate.Id, leanOrderStatus, out leanOrder))
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Order not found: {orderUpdate.Id}. Order detail: {orderUpdate}"));
                    break;
                }

                orderEvent = new OrderEvent(leanOrder, orderUpdate.CancelledAtUtc, OrderFee.Zero)
                {
                    Status = leanOrderStatus
                };
                ProcessOrderEventWithCrossZeroCheck(leanOrder, orderEvent);
                break;
            case BrokerageOrderStatus.Expired:
                leanOrderStatus = LeanOrderStatus.Canceled;
                if (!TryGetLeanOrderByBrokerageId(orderUpdate.Id, leanOrderStatus, out leanOrder))
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Order not found: {orderUpdate.Id}. Order detail: {orderUpdate}"));
                    break;
                }

                // TODO: Add missed 'message' in OrderEvent "Why does it expire?"
                orderEvent = new OrderEvent(leanOrder, DateTime.UtcNow, OrderFee.Zero)
                { Status = leanOrderStatus };

                ProcessOrderEventWithCrossZeroCheck(leanOrder, orderEvent);

                break;
        }
    }

    /// <summary>
    /// Finalizes the submission of a pending order identified by its brokerage ID.
    /// If the order is found in the pending cache, it removes the entry, optionally emits 
    /// a <see cref="OrderEvent"/>, and signals any thread waiting on the order's completion.
    /// </summary>
    /// <param name="brokerageId">The unique brokerage-assigned order ID.</param>
    /// <param name="receivedDateTime">The timestamp indicating when the order was acknowledged by the brokerage.</param>
    private void ProcessPendingOrderSubmission(string brokerageId, DateTime receivedDateTime)
    {
        if (!_pendingOrderCache.IsEmpty && _pendingOrderCache.TryRemove(brokerageId, out var orderManager))
        {
            orderManager.AutoResetEvent.Set();

            if (orderManager.IsInvokeOrderEvent)
            {
                var orderEvent = new OrderEvent(orderManager.LeanOrder, receivedDateTime, OrderFee.Zero)
                {
                    Status = orderManager.InvokeOrderStatus
                };

                ProcessOrderEventWithCrossZeroCheck(orderManager.LeanOrder, orderEvent);
            }
        }
    }

    /// <summary>
    /// Processes an incoming order event by first attempting to handle it as a cross-zero order.
    /// If the event is not related to a cross-zero scenario, it is passed to the standard order handler.
    /// </summary>
    /// <param name="leanOrder">The Lean order associated with the event.</param>
    /// <param name="orderEvent">The specific order event to process.</param>
    private void ProcessOrderEventWithCrossZeroCheck(LeanOrder leanOrder, OrderEvent orderEvent)
    {
        if (!TryHandleRemainingCrossZeroOrder(leanOrder, orderEvent))
        {
            OnOrderEvent(orderEvent);
        }
    }

    /// <summary>
    /// Attempts to retrieve the corresponding LEAN order for a given brokerage order ID.
    /// </summary>
    /// <param name="brokerageOrderId">The brokerage-assigned order ID used for lookup.</param>
    /// <param name="supposeOrderStatus">
    /// The expected status of the order, used to resolve potential CrossZero orders before fallback.
    /// </param>
    /// <param name="leanOrder">
    /// When this method returns, contains the resolved <see cref="LeanOrder"/> if found; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a matching LEAN order was found for the provided brokerage order ID; otherwise, <c>false</c>.
    /// </returns>
    private bool TryGetLeanOrderByBrokerageId(string brokerageOrderId, LeanOrderStatus supposeOrderStatus, out LeanOrder leanOrder)
    {
        if (!TryGetOrRemoveCrossZeroOrder(brokerageOrderId, supposeOrderStatus, out leanOrder))
        {
            leanOrder = _orderProvider.GetOrdersByBrokerageId(brokerageOrderId).LastOrDefault();
        }

        if (leanOrder == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Converts a LEAN <see cref="LeanOrder"/> into a brokerage-compatible <see cref="OrderBaseRequest"/>.
    /// </summary>
    /// <param name="order">The LEAN order to convert.</param>
    /// <param name="orderQuantity">The quantity of the order to be placed.</param>
    /// <param name="orderAction">The resolved <see cref="OrderAction"/> based on order context and position.</param>
    /// <returns>
    /// A brokerage-specific <see cref="OrderBaseRequest"/> derived from the provided LEAN order.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown if the given order type is not supported for conversion to the brokerage model.
    /// </exception>
    private OrderBaseRequest ConvertLeanOrderToBrokerageOrder(LeanOrder order, decimal orderQuantity, OrderAction orderAction)
    {
        var (timeInForce, expiryDateTime) = order.Properties.TimeInForce.GetBrokerageTimeInForceByLeanTimeInForce();

        var brokerageSymbol = _symbolMapper.GetBrokerageSymbols(order.Symbol).brokerageSymbol;
        var instrumentType = order.SecurityType.ConvertLeanSecurityTypeToBrokerageInstrumentType();

        var legs = new List<LegAttributes>()
        {
            new (orderAction, instrumentType, Math.Abs(orderQuantity), brokerageSymbol)
        };

        var brokerageOrder = default(OrderBaseRequest);
        switch (order)
        {
            case MarketOrder:
                brokerageOrder = new MarketOrderRequest(legs);
                break;
            case LimitOrder lo:
                brokerageOrder = new LimitOrderRequest(timeInForce, expiryDateTime, legs, lo.LimitPrice, order.Direction);
                break;
            case StopMarketOrder smo:
                brokerageOrder = new StopMarketOrderRequest(timeInForce, expiryDateTime, legs, smo.StopPrice, instrumentType);
                break;
            case StopLimitOrder slo:
                brokerageOrder = new StopLimitOrderRequest(timeInForce, expiryDateTime, legs, slo.LimitPrice, slo.StopPrice, order.Direction);
                break;
            default:
                throw new NotSupportedException($"{nameof(TastytradeBrokerage)}.{nameof(ConvertLeanOrderToBrokerageOrder)}: The order type '{order.GetType().Name}' is not supported for brokerage conversion.");
        }

        return brokerageOrder;
    }

    /// <summary>
    /// Determines the appropriate <see cref="OrderAction"/> for a given order direction,
    /// security type, and current holdings quantity.
    /// </summary>
    /// <param name="orderDirection">The direction of the order (e.g., <see cref="OrderDirection.Buy"/> or <see cref="OrderDirection.Sell"/>).</param>
    /// <param name="securityType">The type of security being traded (e.g., Equity, Option, IndexOption).</param>
    /// <param name="holdingsQuantity">The current quantity of the security held in the portfolio.</param>
    /// <returns>The corresponding <see cref="OrderAction"/> representing the correct market instruction.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the order action cannot be determined due to unsupported position or security type.
    /// </exception>
    private static OrderAction ResolveOrderAction(OrderDirection orderDirection, SecurityType securityType, decimal holdingsQuantity)
    {
        var orderPosition = GetOrderPosition(orderDirection, holdingsQuantity);
        return ResolveOrderAction(securityType, orderPosition);
    }

    /// <summary>
    /// Resolves the appropriate <see cref="OrderAction"/> based on security type and calculated <see cref="OrderPosition"/>.
    /// </summary>
    /// <param name="securityType">The type of security (e.g., Equity, Option, Crypto).</param>
    /// <param name="orderPosition">The logical order position (e.g., BuyToOpen, SellToClose).</param>
    /// <returns>The resolved <see cref="OrderAction"/> matching the security and order context.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown if the <paramref name="orderPosition"/> is not valid for the specified <paramref name="securityType"/>.
    /// </exception>
    private static OrderAction ResolveOrderAction(SecurityType securityType, OrderPosition orderPosition)
    {
        switch (securityType)
        {
            case SecurityType.Equity:
            case SecurityType.Option:
            case SecurityType.IndexOption:
                return orderPosition switch
                {
                    OrderPosition.BuyToOpen => OrderAction.BuyToOpen,
                    OrderPosition.SellToOpen => OrderAction.SellToOpen,
                    OrderPosition.BuyToClose => OrderAction.BuyToClose,
                    OrderPosition.SellToClose => OrderAction.SellToClose,
                    _ => throw new NotSupportedException($"{nameof(TastytradeBrokerage)}.{nameof(ResolveOrderAction)}: The specified order position '{orderPosition}' is not supported.")
                };
            default:
                return orderPosition switch
                {
                    OrderPosition.BuyToOpen or OrderPosition.SellToClose => OrderAction.Buy,
                    OrderPosition.BuyToClose or OrderPosition.SellToOpen => OrderAction.Sell,
                    _ => throw new NotSupportedException($"{nameof(TastytradeBrokerage)}.{nameof(ResolveOrderAction)}: The specified order direction '{orderPosition}' is not supported.")
                };
        }
    }

    /// <summary>
    /// Checks if an exception indicates an unsupported symbol error and optionally raises a warning message. 
    /// If configured to ignore such errors, it logs a warning message instead of rethrowing the exception.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <param name="symbol">The symbol related to the exception.</param>
    /// <param name="instrumentType">The type of instrument for the symbol.</param>
    /// <param name="warningCode">The warning code to include in the brokerage message.</param>
    /// <param name="rethrow">Indicates whether to rethrow the exception if it's not ignored.</param>
    private void CheckUnsupportedSymbolError(Exception exception, string symbol, InstrumentType instrumentType, string warningCode, bool rethrow = true)
    {
        var notSupportedException = exception as NotSupportedException ?? exception.InnerException as NotSupportedException;
        if (notSupportedException != null && _algorithm?.Settings?.IgnoreUnknownAssetHoldings == true)
        {
            lock (_unsupportedSymbols)
            {
                if (_unsupportedSymbols.Add($"{symbol}-{instrumentType}"))
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, warningCode, notSupportedException.Message));
                }
            }
        }
        else if (rethrow)
        {
            throw exception;
        }
    }
}
