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
using LeanOrder = QuantConnect.Orders.Order;
using QuantConnect.Brokerages.Tastytrade.Models;
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
    ///     <description><b>Value:</b> A <see cref="PendingSubmittedOrderManager"/> instance containing the order and an <see cref="AutoResetEvent"/> for signaling when a status update is received.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    private readonly ConcurrentDictionary<string, PendingSubmittedOrderManager> _pendingOrderCache = new();

    /// <summary>
    /// Gets all holdings for the account
    /// </summary>
    /// <returns>The current holdings from the account</returns>
    public override List<Holding> GetAccountHoldings()
    {
        var positions = _tastytradeApiClient.GetAccountPositions().SynchronouslyAwaitTaskResult();

        if (positions.Count == 0)
        {
            return [];
        }

        var holdings = new List<Holding>();
        foreach (var position in positions)
        {
            if (!TryGetLeanSymbol(position.Symbol, position.InstrumentType, out var leanSymbol, out var exceptionMessage, position.UnderlyingSymbol))
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, 1, $"{exceptionMessage}. Position details: {position}."));
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
    /// <param name="exceptionMessage">
    /// An error message describing the failure reason if the mapping is unsuccessful. 
    /// This is an output parameter.
    /// </param>
    /// <param name="optionUnderlyingSymbol">
    /// The underlying symbol for options, used to distinguish between regular options 
    /// and index options. This parameter is optional and defaults to <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the mapping is successful; otherwise, <c>false</c>.
    /// </returns>
    internal bool TryGetLeanSymbol(string symbol, InstrumentType instrumentType, out Symbol leanSymbol, out string exceptionMessage, string optionUnderlyingSymbol = null)
    {
        leanSymbol = default;
        exceptionMessage = default;
        try
        {
            leanSymbol = _symbolMapper.GetLeanSymbol(symbol, instrumentType.ConvertInstrumentTypeToSecurityType(), optionUnderlyingSymbol);
            return true;
        }
        catch (Exception ex)
        {
            exceptionMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Gets the current cash balance for each currency held in the brokerage account
    /// </summary>
    /// <returns>The current cash balance for each currency available for trading</returns>
    public override List<CashAmount> GetCashBalance()
    {
        var balance = _tastytradeApiClient.GetAccountBalances().SynchronouslyAwaitTaskResult();
        return [new(balance.AvailableTradingFunds, balance.Currency)];
    }

    /// <summary>
    /// Gets all open orders on the account.
    /// NOTE: The order objects returned do not have QC order IDs.
    /// </summary>
    /// <returns>The open orders returned from IB</returns>
    public override List<LeanOrder> GetOpenOrders()
    {
        var brokerageOrders = _tastytradeApiClient.GetLiveOrders().SynchronouslyAwaitTaskResult();

        if (brokerageOrders.Count == 0)
        {
            return [];
        }

        var leanOrders = new List<LeanOrder>();
        foreach (var brokerageOrder in brokerageOrders)
        {
            var orderProperties = new OrderProperties();
            // TODO: missed DateTime parameter in TryGetLeanTimeInForce, test with GTD timeInForce
            if (!orderProperties.TryGetLeanTimeInForce(brokerageOrder.TimeInForce, default))
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
        if (!TryGetLeanSymbol(leg.Symbol, leg.InstrumentType, out var leanSymbol, out var exceptionMessage, order.UnderlyingSymbol))
        {
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, 1, $"{exceptionMessage}. Order details: {order}."));
            return false;
        }

        var quantity = leg.Action.ToSignedQuantity(leg.Quantity);
        switch (order.OrderType)
        {
            case Models.Enum.OrderType.Market:
                leanOrder = new MarketOrder(leanSymbol, quantity, order.ReceivedAt, properties: orderProperties);
                break;
            case Models.Enum.OrderType.Limit:
                leanOrder = new LimitOrder(leanSymbol, quantity, order.Price, order.ReceivedAt, properties: orderProperties);
                break;
        }

        if (leanOrder == default)
        {
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Skipping unsupported order type '{order.OrderType}'. Order details: {order}."));
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

        var brokerageOrder = ConvertLeanOrderToBrokerageOrder(order);

        var brokerageId = default(string);
        var isPlaced = default(bool);
        var pendingSubmittedOrder = new PendingSubmittedOrderManager(order);
        _messageHandler.WithLockedStream(() =>
        {
            try
            {
                brokerageId = _tastytradeApiClient.SubmitOrder(brokerageOrder).SynchronouslyAwaitTaskResult().Order.Id;
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

        return true;
    }

    /// <summary>
    /// Updates the order with the same id
    /// </summary>
    /// <param name="order">The new order information</param>
    /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
    public override bool UpdateOrder(LeanOrder order)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Cancels the order with the specified ID
    /// </summary>
    /// <param name="order">The order to cancel</param>
    /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
    public override bool CancelOrder(LeanOrder order)
    {
        if (order.Status == LeanOrderStatus.Filled || order.Status == LeanOrderStatus.Canceled)
        {
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, "Cancel Order: Order already " + order.Status));
            return false;
        }

        var canceled = default(bool);
        var brokerageId = order.BrokerId.FirstOrDefault();
        _messageHandler.WithLockedStream(() =>
        {
            try
            {
                _tastytradeApiClient.CancelOrderById(brokerageId).SynchronouslyAwaitTask();
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
    /// Handles updates to brokerage orders received from the external brokerage system. 
    /// Updates the corresponding Lean order status and generates <see cref="OrderEvent"/>s as appropriate.
    /// </summary>
    /// <param name="orderUpdate">The updated <see cref="BrokerageOrder"/> received from the brokerage.</param>
    /// <remarks>
    /// Supported brokerage order statuses:
    /// <list type="bullet">
    /// <item><description><see cref="BrokerageOrderStatus.Routed"/> and <see cref="BrokerageOrderStatus.Received"/>: Checks for market open status and defers processing if closed.</description></item>
    /// <item><description><see cref="BrokerageOrderStatus.Live"/>: Immediately processes pending submission.</description></item>
    /// <item><description><see cref="BrokerageOrderStatus.Filled"/>: Creates and emits a filled <see cref="OrderEvent"/>.</description></item>
    /// <item><description><see cref="BrokerageOrderStatus.Cancelled"/>: Creates and emits a canceled <see cref="OrderEvent"/>.</description></item>
    /// <item><description><see cref="BrokerageOrderStatus.Expired"/>: Emits a canceled <see cref="OrderEvent"/> to reflect expiration (message TODO).</description></item>
    /// </list>
    /// Emits a warning message if the order cannot be matched with an existing Lean order.
    /// </remarks>
    protected override void OnOrderUpdateReceived(BrokerageOrder orderUpdate)
    {
        switch (orderUpdate.Status)
        {
            case BrokerageOrderStatus.Routed:
            case BrokerageOrderStatus.Received:
                if (!TryGetLeanOrderByBrokerageId(orderUpdate.Id, out var leanOrder))
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Order not found: {orderUpdate.Id}. Order detail: {orderUpdate}"));
                    break;
                }

                // Check if the market is currently closed for this symbol
                // Note: This is a workaround for placing orders outside of regular market hours
                // TODO: Proper extended market hours support should be implemented
                if (!leanOrder.Symbol.IsMarketOpen(DateTime.UtcNow, false))
                {
                    ProcessPendingOrderSubmission(orderUpdate.Id, orderUpdate.ReceivedAt);
                }
                break;
            case BrokerageOrderStatus.Live:
                ProcessPendingOrderSubmission(orderUpdate.Id, orderUpdate.ReceivedAt);
                break;
            case BrokerageOrderStatus.Filled:
                if (!TryGetLeanOrderByBrokerageId(orderUpdate.Id, out leanOrder))
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Order not found: {orderUpdate.Id}. Order detail: {orderUpdate}"));
                    break;
                }

                var leg = orderUpdate.Legs.FirstOrDefault();
                var fill = leg.Fills.FirstOrDefault();
                var orderEvent = new OrderEvent(leanOrder, fill.FilledAt, OrderFee.Zero)
                {
                    Status = LeanOrderStatus.Filled,
                    FillQuantity = leg.Action.ToSignedQuantity(fill.Quantity),
                    FillPrice = fill.FillPrice
                };
                OnOrderEvent(orderEvent);
                break;
            case BrokerageOrderStatus.Cancelled:
                if (!TryGetLeanOrderByBrokerageId(orderUpdate.Id, out leanOrder))
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Order not found: {orderUpdate.Id}. Order detail: {orderUpdate}"));
                    break;
                }

                orderEvent = new OrderEvent(leanOrder, orderUpdate.CancelledAt, OrderFee.Zero)
                {
                    Status = LeanOrderStatus.Canceled
                };
                OnOrderEvent(orderEvent);
                break;
            case BrokerageOrderStatus.Expired:
                if (!TryGetLeanOrderByBrokerageId(orderUpdate.Id, out leanOrder))
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Order not found: {orderUpdate.Id}. Order detail: {orderUpdate}"));
                    break;
                }

                // TODO: Add missed 'message' in OrderEvent "Why does it expire?"
                orderEvent = new OrderEvent(leanOrder, DateTime.UtcNow, OrderFee.Zero)
                { Status = LeanOrderStatus.Canceled };

                OnOrderEvent(orderEvent);

                break;
        }
    }

    /// <summary>
    /// Processes and finalizes the submission of a pending order.
    /// If the order exists in the pending order cache, it removes it,
    /// emits a submitted order event, and signals any waiting thread.
    /// </summary>
    /// <param name="brokerageId">The unique brokerage order ID.</param>
    /// <param name="receivedDateTime">The timestamp when the order was received.</param>
    private void ProcessPendingOrderSubmission(string brokerageId, DateTime receivedDateTime)
    {
        if (_pendingOrderCache.TryRemove(brokerageId, out var orderManager))
        {
            OnOrderEvent(new OrderEvent(orderManager.LeanOrder, receivedDateTime, OrderFee.Zero, "Place Order Event: Submitted")
            {
                Status = LeanOrderStatus.Submitted
            });
            orderManager.AutoResetEvent.Set();
        }
    }

    /// <summary>
    /// Attempts to retrieve a Lean order by its brokerage order ID.
    /// </summary>
    /// <param name="brokerageOrderId">The brokerage order ID to search for.</param>
    /// <param name="leanOrder">
    /// When this method returns, contains the Lean order associated with the specified brokerage order ID, 
    /// if found; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if an order with the specified brokerage ID is found; otherwise, <c>false</c>.
    /// </returns>
    private bool TryGetLeanOrderByBrokerageId(string brokerageOrderId, out LeanOrder leanOrder)
    {
        leanOrder = _orderProvider.GetOrdersByBrokerageId(brokerageOrderId).FirstOrDefault();

        if (leanOrder == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Converts a LEAN <see cref="Order"/> into a brokerage-compatible <see cref="OrderBaseRequest"/>.
    /// </summary>
    /// <param name="order">The LEAN order to convert.</param>
    /// <returns>
    /// A brokerage-specific order request corresponding to the provided LEAN order.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the order type is not supported for conversion.
    /// </exception>
    private OrderBaseRequest ConvertLeanOrderToBrokerageOrder(LeanOrder order)
    {
        var (timeInForce, expiryDateTime) = order.Properties.TimeInForce.GetBrokerageTimeInForceByLeanTimeInForce();

        var holdingQuantity = _securityProvider.GetHoldingsQuantity(order.Symbol);
        var orderAction = GetOrderActionByDirection(order.Direction, order.SecurityType, holdingQuantity);
        var brokerageSymbol = _symbolMapper.GetBrokerageSymbols(order.Symbol).brokerageSymbol;
        var instrumentType = order.SecurityType.ConvertLeanSecurityTypeToBrokerageInstrumentType();

        var legs = new List<LegAttributes>()
        {
            new (orderAction, instrumentType, order.AbsoluteQuantity, brokerageSymbol)
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
                brokerageOrder = new StopMarketOrderRequest(timeInForce, expiryDateTime, legs, smo.StopPrice);
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
    /// Resolves the appropriate <see cref="OrderAction"/> based on the order direction,
    /// security type, and current holdings.
    /// </summary>
    /// <param name="orderDirection">The direction of the order (e.g., <see cref="OrderDirection.Buy"/> or <see cref="OrderDirection.Sell"/>).</param>
    /// <param name="securityType">The type of security being traded (e.g., Equity, Option, IndexOption).</param>
    /// <param name="holdingsQuantity">The current number of units held for the given security.</param>
    /// <returns>
    /// A corresponding <see cref="OrderAction"/> that reflects the correct market instruction.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specified order position or direction is not supported for the given security type.
    /// </exception>
    private static OrderAction GetOrderActionByDirection(OrderDirection orderDirection, SecurityType securityType, decimal holdingsQuantity)
    {
        var orderPosition = GetOrderPosition(orderDirection, holdingsQuantity);

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
                    _ => throw new NotSupportedException($"{nameof(TastytradeBrokerage)}.{nameof(GetOrderActionByDirection)}: The specified order position '{orderPosition}' is not supported.")
                };
            default:
                return orderDirection switch
                {
                    OrderDirection.Sell => OrderAction.Sell,
                    OrderDirection.Buy => OrderAction.Buy,
                    _ => throw new NotSupportedException($"{nameof(TastytradeBrokerage)}.{nameof(GetOrderActionByDirection)}: The specified order direction '{orderDirection}' is not supported.")
                };
        }
    }
}
