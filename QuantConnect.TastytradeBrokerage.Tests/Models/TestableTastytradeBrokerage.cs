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

using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Tests.Engine.DataFeeds;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Tastytrade.Tests.Models;

/// <summary>
/// Brokerage that lets a test reach the protected socket handlers directly and reports what it is doing,
/// so the test steps stand out in a log that is otherwise full of framework messages.
/// </summary>
public class TestableTastytradeBrokerage : TastytradeBrokerage
{
    /// <summary>
    /// The line written above and below every <see cref="LogStep"/> message, so a test step never runs
    /// into the framework logs around it.
    /// </summary>
    private const string LogSeparator = "------------------------------------------------------------";

    /// <summary>
    /// The Lean orders of this brokerage. The brokerage keeps it the way Lean's transaction handler does:
    /// an order placed outside the algorithm gets its Lean id as soon as it is offered, and every order event
    /// lands on the order and on its ticket. It is <c>null</c> for a brokerage without any connection.
    /// </summary>
    public OrderProvider OrderProvider { get; }

    /// <summary>
    /// The holdings this brokerage reads to pick the buy or sell instruction of an order. The brokerage keeps it
    /// the way Lean's portfolio does: every fill moves the holdings of its security.
    /// It is <c>null</c> for a brokerage without any connection.
    /// </summary>
    public ISecurityProvider SecurityProvider { get; }

    /// <summary>
    /// What the algorithm answers when an order placed outside of it is offered. The tests accept such
    /// an order unless they say otherwise; Lean's default brokerage message handler declines it.
    /// </summary>
    public bool AcceptBrokerageSideOrders { get; init; } = true;

    /// <summary>
    /// Creates a brokerage without any connection: the configuration is not read and Lean is not asked about the subscription.
    /// </summary>
    public static TestableTastytradeBrokerage CreateWithoutConnection()
    {
        return new TestableTastytradeBrokerage();
    }

    /// <summary>
    /// Starts logging the brokerage messages, the only setup a brokerage without any connection needs.
    /// </summary>
    private TestableTastytradeBrokerage()
    {
        Message += OnBrokerageMessage;
    }

    /// <summary>
    /// Creates a brokerage for the account in Tests/config.json; <see cref="Connect"/> opens the real sockets.
    /// </summary>
    /// <param name="orderProvider">The order provider that holds the Lean orders; a new one when <c>null</c>.</param>
    /// <param name="securityProvider">The security provider that holds the holdings; a new one when <c>null</c>.</param>
    public TestableTastytradeBrokerage(OrderProvider orderProvider = null, ISecurityProvider securityProvider = null)
        : this()
    {
        orderProvider ??= new OrderProvider();
        securityProvider ??= new SecurityProvider();

        OrderProvider = orderProvider;
        SecurityProvider = securityProvider;
        Initialize(Config.Get("tastytrade-api-url"), Config.Get("tastytrade-websocket-url"), Config.Get("tastytrade-username"), Config.Get("tastytrade-password"),
            Config.Get("tastytrade-account-number"), Config.Get("tastytrade-refresh-token"), orderProvider, securityProvider, new AlgorithmStub());
    }

    /// <summary>
    /// Delivers an account stream message the way the account socket does.
    /// </summary>
    /// <param name="message">The JSON text of the message.</param>
    public void ReceiveAccountStreamMessage(string message)
    {
        LogStep("delivering a captured account stream message...");
        OnAccountUpdateMessageHandler(this, new WebSocketMessage(null, new WebSocketClientWrapper.TextMessage { Message = message }));
    }

    /// <summary>
    /// Reports a socket status change the way the socket wrappers and the stream handlers do.
    /// </summary>
    /// <param name="sender">The socket that reports it.</param>
    /// <param name="messageType"><see cref="BrokerageMessageType.Disconnect"/> or <see cref="BrokerageMessageType.Reconnect"/>.</param>
    /// <param name="reason">What happened to the socket.</param>
    public void ReportConnectionStatus(object sender, BrokerageMessageType messageType, string reason)
    {
        OnConnectionStatusChanged(sender, messageType, reason);
    }

    /// <summary>
    /// Logs the connect and what the sockets did.
    /// </summary>
    public override void Connect()
    {
        LogStep("connecting...");
        base.Connect();
        LogStep($"connected: {IsConnected}.");
    }

    /// <summary>
    /// Logs the disconnect.
    /// </summary>
    public override void Disconnect()
    {
        LogStep("disconnecting.");
        base.Disconnect();
    }

    /// <summary>
    /// Logs the order this brokerage sends to Tastytrade.
    /// </summary>
    /// <param name="order">The order to place.</param>
    public override bool PlaceOrder(Order order)
    {
        LogStep($"placing the {order.Type} order for {order.Symbol.Value}...");
        return base.PlaceOrder(order);
    }

    /// <summary>
    /// Logs the order this brokerage cancels.
    /// </summary>
    /// <param name="order">The order to cancel.</param>
    public override bool CancelOrder(Order order)
    {
        LogStep($"canceling the {order.Type} order for {order.Symbol.Value}, BrokerId: {string.Join(", ", order.BrokerId)}...");
        return base.CancelOrder(order);
    }

    /// <summary>
    /// Logs every order placed outside the algorithm. With <see cref="AcceptBrokerageSideOrders"/> it accepts the order
    /// the way the transaction handler does when the algorithm takes it: the order gets its Lean id.
    /// </summary>
    /// <param name="e">The order that is offered.</param>
    protected override void OnNewBrokerageOrderNotification(NewBrokerageOrderNotificationEventArgs e)
    {
        LogStep($"offering the order placed outside the algorithm: {e.Order}, BrokerId: {string.Join(", ", e.Order.BrokerId)}");
        if (AcceptBrokerageSideOrders)
        {
            OrderProvider?.Add(e.Order);
        }
        base.OnNewBrokerageOrderNotification(e);
    }

    /// <summary>
    /// Logs every order event together with the brokerage ids of the order behind it. Before the test hears
    /// about the event, the status is on the order, the event is on its ticket and a fill is in the holdings.
    /// </summary>
    /// <param name="orderEvents">The order events reported by the brokerage.</param>
    protected override void OnOrderEvents(List<OrderEvent> orderEvents)
    {
        foreach (var orderEvent in orderEvents)
        {
            var brokerIds = OrderProvider?.GetOrderById(orderEvent.OrderId)?.BrokerId ?? [];
            LogStep($"order event {orderEvent}, BrokerId: {string.Join(", ", brokerIds)}");
            OrderProvider?.HandleOrderEvent(orderEvent);

            if (SecurityProvider != null && orderEvent.Status is OrderStatus.Filled or OrderStatus.PartiallyFilled)
            {
                var holdings = SecurityProvider.GetSecurity(orderEvent.Symbol).Holdings;
                holdings.SetHoldings(orderEvent.FillPrice, holdings.Quantity + orderEvent.FillQuantity);
                LogStep($"holdings {holdings}");
            }
        }

        base.OnOrderEvents(orderEvents);
    }

    /// <summary>
    /// Writes one line between two marker lines. Search the log for <c>&gt;&gt;&gt;&gt;</c> to read only these lines.
    /// </summary>
    /// <param name="message">What the brokerage is doing.</param>
    public void LogStep(string message)
    {
        Log.Trace(LogSeparator);
        Log.Trace($">>>> {message}");
        Log.Trace(LogSeparator);
    }

    /// <summary>
    /// Logs every brokerage message before it reaches the test. Subscribed to <see cref="Brokerage.Message"/>
    /// rather than overriding <see cref="Brokerage.OnMessage(BrokerageMessageEvent)"/>, so a Disconnect or Reconnect that Lean's
    /// guard drops is not logged as if it had happened.
    /// </summary>
    /// <param name="_">The brokerage that reported the message. This parameter is not used.</param>
    /// <param name="e">The message Lean let through.</param>
    private void OnBrokerageMessage(object _, BrokerageMessageEvent e)
    {
        LogStep($"message {e}");
    }
}
