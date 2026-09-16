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
using NUnit.Framework;
using System.Threading;
using QuantConnect.Tests;
using QuantConnect.Orders;
using QuantConnect.Logging;
using System.Collections.Generic;
using QuantConnect.Tests.Brokerages;
using OrderAction = QuantConnect.Brokerages.Tastytrade.Models.Enum.OrderAction;
using InstrumentType = QuantConnect.Brokerages.Tastytrade.Models.Enum.InstrumentType;
using Leg = QuantConnect.Brokerages.Tastytrade.Models.Orders.Leg;
using Fill = QuantConnect.Brokerages.Tastytrade.Models.Orders.Fill;
using BrokerageOrder = QuantConnect.Brokerages.Tastytrade.Models.Orders.Order;
using BrokerageOrderType = QuantConnect.Brokerages.Tastytrade.Models.Enum.OrderType;
using BrokerageOrderStatus = QuantConnect.Brokerages.Tastytrade.Models.Enum.OrderStatus;
using BrokerageTimeInForce = QuantConnect.Brokerages.Tastytrade.Models.Enum.TimeInForce;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

/// <summary>
/// Orders placed in the account outside the algorithm reach the account stream like any other order.
/// The brokerage offers each of them once to the algorithm through <see cref="Brokerage.NewBrokerageOrderNotification"/>.
/// </summary>
[TestFixture]
public class TastytradeBrokerageSideOrderTests
{
    private const string BrokerageOrderId = "270602";

    private OrderProvider _orderProvider;
    private TastytradeBrokerage _brokerage;
    private List<Order> _offeredOrders;
    private List<OrderEvent> _orderEvents;
    private List<BrokerageMessageEvent> _messages;

    [SetUp]
    public void SetUp()
    {
        _orderProvider = new OrderProvider();
        _brokerage = TestSetup.CreateBrokerage(_orderProvider, new SecurityProvider());
        _offeredOrders = [];
        _orderEvents = [];
        _messages = [];
        _brokerage.NewBrokerageOrderNotification += (_, e) => _offeredOrders.Add(e.Order);
        _brokerage.OrdersStatusChanged += (_, events) => _orderEvents.AddRange(events);
        _brokerage.Message += (_, message) => _messages.Add(message);
    }

    [TearDown]
    public void TearDown()
    {
        _brokerage.Dispose();
    }

    [Test]
    public void OffersFilledOrderOnceAndReportsItsFill()
    {
        AcceptOfferedOrders();

        _brokerage.OnOrderUpdateReceived(CreateOrderUpdate(BrokerageOrderType.Market, BrokerageOrderStatus.Filled, 1m, CreateFill("fill-1", 1m, 1m)));

        var offeredOrder = _offeredOrders.Single();
        Assert.That(offeredOrder.Type, Is.EqualTo(OrderType.Market));
        Assert.That(offeredOrder.Symbol.Value, Is.EqualTo("AAPL"));
        Assert.That(offeredOrder.Quantity, Is.EqualTo(1m));
        Assert.That(offeredOrder.BrokerId.Single(), Is.EqualTo(BrokerageOrderId));
        Assert.That(_orderProvider.GetOrdersByBrokerageId(BrokerageOrderId).Single().Id, Is.EqualTo(offeredOrder.Id));

        Assert.That(_orderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.Submitted, OrderStatus.Filled }));
        Assert.That(_orderEvents[0].Message, Is.EqualTo("Order was submitted outside Lean"));
        Assert.That(_orderEvents[1].FillQuantity, Is.EqualTo(1m));
        Assert.That(_orderEvents[1].FillPrice, Is.EqualTo(1m));
        Assert.That(_messages, Is.Empty);
    }

    [Test]
    public void OffersLiveOrderOnceAndTracksItsLaterFill()
    {
        AcceptOfferedOrders();

        _brokerage.OnOrderUpdateReceived(CreateOrderUpdate(BrokerageOrderType.Market, BrokerageOrderStatus.Live, 1m));
        Assert.That(_offeredOrders, Has.Count.EqualTo(1));
        Assert.That(_orderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.Submitted }));

        _brokerage.OnOrderUpdateReceived(CreateOrderUpdate(BrokerageOrderType.Market, BrokerageOrderStatus.Filled, 1m, CreateFill("fill-1", 1m, 1m)));
        Assert.That(_offeredOrders, Has.Count.EqualTo(1));
        Assert.That(_orderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.Submitted, OrderStatus.Filled }));
        Assert.That(_orderEvents[1].OrderId, Is.EqualTo(_offeredOrders[0].Id));
    }

    [Test]
    public void ReportsEveryFillOfPartiallyFilledOrder()
    {
        AcceptOfferedOrders();

        _brokerage.OnOrderUpdateReceived(CreateOrderUpdate(BrokerageOrderType.Market, BrokerageOrderStatus.Filled, 2m, CreateFill("fill-1", 1m, 1m), CreateFill("fill-2", 1m, 1.1m)));

        Assert.That(_orderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.Submitted, OrderStatus.PartiallyFilled, OrderStatus.Filled }));
        Assert.That(_orderEvents[1].FillQuantity, Is.EqualTo(1m));
        Assert.That(_orderEvents[1].FillPrice, Is.EqualTo(1m));
        Assert.That(_orderEvents[2].FillQuantity, Is.EqualTo(1m));
        Assert.That(_orderEvents[2].FillPrice, Is.EqualTo(1.1m));
    }

    [Test]
    public void WarnsOnceAboutDeclinedOrderAndDoesNotOfferItAgain()
    {
        // The default brokerage message handler declines the order, so Lean assigns no order id.
        _brokerage.OnOrderUpdateReceived(CreateOrderUpdate(BrokerageOrderType.Market, BrokerageOrderStatus.Live, 1m));
        _brokerage.OnOrderUpdateReceived(CreateOrderUpdate(BrokerageOrderType.Market, BrokerageOrderStatus.Filled, 1m, CreateFill("fill-1", 1m, 1m)));

        Assert.That(_offeredOrders, Has.Count.EqualTo(1));
        Assert.That(_offeredOrders[0].Id, Is.EqualTo(0));
        Assert.That(_orderEvents, Is.Empty);
        var warning = _messages.Single();
        Assert.That(warning.Type, Is.EqualTo(BrokerageMessageType.Warning));
        Assert.That(warning.Code, Is.EqualTo("UnknownOrderId"));
        Assert.That(warning.Message, Does.Contain(BrokerageOrderId));
    }

    [Test]
    public void WarnsAboutUnsupportedOrderTypeWithoutOfferingIt()
    {
        AcceptOfferedOrders();

        _brokerage.OnOrderUpdateReceived(CreateOrderUpdate(BrokerageOrderType.NotionalMarket, BrokerageOrderStatus.Live, 1m));

        Assert.That(_offeredOrders, Is.Empty);
        Assert.That(_orderEvents, Is.Empty);
        var warning = _messages.Single();
        Assert.That(warning.Type, Is.EqualTo(BrokerageMessageType.Warning));
        Assert.That(warning.Code, Is.EqualTo("UnsupportedOrderType"));
        Assert.That(warning.Message, Does.Contain(BrokerageOrderId));
    }

    [Test]
    public void DoesNotOfferOrderPlacedByLean()
    {
        var leanOrder = new MarketOrder(Symbols.AAPL, 1m, new DateTime(2025, 5, 28, 16, 0, 1, DateTimeKind.Utc));
        leanOrder.BrokerId.Add(BrokerageOrderId);
        _orderProvider.Add(leanOrder);

        _brokerage.OnOrderUpdateReceived(CreateOrderUpdate(BrokerageOrderType.Market, BrokerageOrderStatus.Filled, 1m, CreateFill("fill-1", 1m, 1m)));

        Assert.That(_offeredOrders, Is.Empty);
        Assert.That(_messages, Is.Empty);
        var orderEvent = _orderEvents.Single();
        Assert.That(orderEvent.OrderId, Is.EqualTo(leanOrder.Id));
        Assert.That(orderEvent.Status, Is.EqualTo(OrderStatus.Filled));
    }

    /// <summary>
    /// Runs against the account in Tests/config.json. Once the log shows the account stream is connected,
    /// place a Limit order in the Tastytrade app that stays open (sandbox: price above $3) within one minute.
    /// The stream update for the new order id is offered to the algorithm once and reported as submitted.
    /// The algorithm then owns the order, so the test cancels it through the brokerage and expects the
    /// canceled event from the stream. The lines prefixed <c>BrokerageSideOrder:</c> show what arrived.
    /// </summary>
    [Test, Explicit("Requires valid Tastytrade credentials and a Limit order placed in the Tastytrade app while the test waits one minute; the test cancels that order.")]
    public void LiveOffersLimitOrderPlacedInTheAppAndCancelsIt()
    {
        AcceptOfferedOrders();
        using var submitted = new ManualResetEventSlim(false);
        using var canceled = new ManualResetEventSlim(false);
        _brokerage.NewBrokerageOrderNotification += (_, e) => Log.Trace($"BrokerageSideOrder: offered {e.Order}");
        _brokerage.OrdersStatusChanged += (_, events) =>
        {
            foreach (var orderEvent in events)
            {
                Log.Trace($"BrokerageSideOrder: {orderEvent}");
                switch (orderEvent.Status)
                {
                    case OrderStatus.Submitted:
                        submitted.Set();
                        break;
                    case OrderStatus.Canceled:
                        canceled.Set();
                        break;
                }
            }
        };
        _brokerage.Message += (_, message) => Log.Trace($"BrokerageSideOrder: {message.Type} {message.Code}: {message.Message}");

        _brokerage.Connect();
        Assert.That(_brokerage.IsConnected, Is.True);
        try
        {
            Log.Trace("BrokerageSideOrder: connected, place the Limit order in the Tastytrade app now (one minute)");
            Assert.That(submitted.Wait(TimeSpan.FromMinutes(1)), Is.True, "No submitted order event arrived within one minute.");

            var offeredOrder = _offeredOrders.Single();
            Assert.That(offeredOrder.Type, Is.EqualTo(OrderType.Limit));
            Assert.That(offeredOrder.Id, Is.Not.EqualTo(0));

            // The REST answer for the same id is the reference for what the stream update was converted into.
            var brokerageOrderId = offeredOrder.BrokerId.Single();
            var appOrder = TestSetup.CreateTastytradeApiClient().GetLiveOrders().SingleOrDefault(order => order.Id == brokerageOrderId);
            Assert.That(appOrder, Is.Not.Null, $"Order {brokerageOrderId} is no longer open; place a Limit order that stays open (sandbox: price above $3).");
            var leg = appOrder.Legs.Single();
            Assert.That(((LimitOrder)offeredOrder).LimitPrice, Is.EqualTo(appOrder.Price));
            Assert.That(offeredOrder.Quantity, Is.EqualTo(leg.Action.ToSignedQuantity(leg.Quantity)));
            Assert.That(_brokerage.TryGetLeanSymbol(leg.Symbol, leg.InstrumentType, out var expectedSymbol, appOrder.UnderlyingSymbol), Is.True);
            Assert.That(offeredOrder.Symbol, Is.EqualTo(expectedSymbol));

            // Lean owns the order now, so it is cancelled the way the algorithm would cancel any of its orders.
            Assert.That(_brokerage.CancelOrder(offeredOrder), Is.True);
            Assert.That(canceled.Wait(TimeSpan.FromSeconds(30)), Is.True, "No canceled order event arrived within 30 seconds.");
        }
        finally
        {
            _brokerage.Disconnect();
        }

        Assert.That(_orderEvents.Select(orderEvent => orderEvent.OrderId), Is.All.EqualTo(_offeredOrders[0].Id));
        Assert.That(_orderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.Submitted, OrderStatus.Canceled }));
        Assert.That(_orderEvents[0].Message, Is.EqualTo("Order was submitted outside Lean"));
        Assert.That(_messages.Where(message => message.Type == BrokerageMessageType.Error), Is.Empty);
    }

    /// <summary>
    /// Does what the transaction handler does when the algorithm accepts the order: assigns its Lean id.
    /// </summary>
    private void AcceptOfferedOrders()
    {
        _brokerage.NewBrokerageOrderNotification += (_, e) => _orderProvider.Add(e.Order);
    }

    /// <summary>
    /// Builds the account stream order update the way Tastytrade sends it for a one-leg AAPL buy,
    /// see <see cref="TastytradeJsonConverterTests.DeserializeStreamFilledOrderMessage"/> for the captured message.
    /// </summary>
    private static BrokerageOrder CreateOrderUpdate(BrokerageOrderType orderType, BrokerageOrderStatus status, decimal quantity, params Fill[] fills)
    {
        return new BrokerageOrder
        {
            Id = BrokerageOrderId,
            OrderType = orderType,
            Status = status,
            TimeInForce = BrokerageTimeInForce.Day,
            ReceivedAt = new DateTimeOffset(2025, 5, 28, 16, 0, 1, 651, TimeSpan.Zero),
            UnderlyingSymbol = "AAPL",
            Legs =
            [
                new Leg
                {
                    Action = OrderAction.BuyToOpen,
                    InstrumentType = InstrumentType.Equity,
                    Symbol = "AAPL",
                    Quantity = quantity,
                    RemainingQuantity = quantity - fills.Sum(fill => fill.Quantity),
                    Fills = fills
                }
            ]
        };
    }

    private static Fill CreateFill(string fillId, decimal quantity, decimal fillPrice)
    {
        return new Fill
        {
            FillId = fillId,
            Quantity = quantity,
            FillPrice = fillPrice,
            FilledAt = new DateTime(2025, 5, 28, 16, 0, 1, 953, DateTimeKind.Utc)
        };
    }
}
