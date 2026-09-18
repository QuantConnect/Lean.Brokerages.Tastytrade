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
using System.Net.Http;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Tests.Models;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

/// <summary>
/// Orders placed in the account outside the algorithm reach the account stream like any other order.
/// The brokerage notifies the algorithm about each of them once through <see cref="Brokerage.NewBrokerageOrderNotification"/>.
/// </summary>
[TestFixture]
public class TastytradeOnNewBrokerageOrderNotificationTests
{
    /// <summary>
    /// Account stream messages of one NOK Limit order at $10.51 placed in the Tastytrade web app (source <c>WB2;0.174.2</c>)
    /// that filled at $10.505, from Routed to Filled, in the order the socket sent them. The Live message already carries the fill.
    /// Captured from the live account stream on 2026-09-17; only the account number is masked.
    /// </summary>
    private static readonly string[] CapturedLimitOrderMessages =
    [
        // Routed
        """{"type":"Order","data":{"id":507349840,"account-number":"5WY00000","cancellable":false,"editable":false,"edited":false,"global-request-id":"2d125a3388db9cabb7b824d3fdab707f","leg-count":1,"order-type":"Limit","price":"10.51","price-effect":"Debit","received-at":"2026-09-17T18:32:28.393+00:00","size":1,"source":"WB2;0.174.2","status":"Routed","time-in-force":"Day","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789669948413,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"timestamp":1789669948424,"ws-sequence":2}""",
        // Live, the fill is already on the leg
        """{"type":"Order","data":{"id":507349840,"account-number":"5WY00000","cancellable":true,"editable":true,"edited":false,"ext-client-order-id":"JAAAC1DXlca8hLdo0w","global-request-id":"2d125a3388db9cabb7b824d3fdab707f","leg-count":1,"order-type":"Limit","price":"10.51","price-effect":"Debit","received-at":"2026-09-17T18:32:28.393+00:00","size":1,"source":"WB2;0.174.2","status":"Live","time-in-force":"Day","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789669948464,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":0,"symbol":"NOK","fills":[{"destination-venue":"SUSQUEHANNA_EQUITIES_B","fill-id":"2609179990014993642","fill-price":"10.505","filled-at":"2026-09-17T18:32:28.435+00:00","quantity":1}]}]},"timestamp":1789669948469,"ws-sequence":3}""",
        // Filled
        """{"type":"Order","data":{"id":507349840,"account-number":"5WY00000","cancellable":false,"editable":false,"edited":false,"ext-client-order-id":"JAAAC1DXlca8hLdo0w","global-request-id":"2d125a3388db9cabb7b824d3fdab707f","leg-count":1,"order-type":"Limit","price":"10.51","price-effect":"Debit","received-at":"2026-09-17T18:32:28.393+00:00","size":1,"source":"WB2;0.174.2","status":"Filled","terminal-at":"2026-09-17T18:32:28.468+00:00","time-in-force":"Day","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789669948471,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":0,"symbol":"NOK","fills":[{"destination-venue":"SUSQUEHANNA_EQUITIES_B","fill-id":"2609179990014993642","fill-price":"10.505","filled-at":"2026-09-17T18:32:28.435+00:00","quantity":1}]}]},"timestamp":1789669948487,"ws-sequence":4}"""
    ];

    [Test, Explicit("Places a real NOK Limit order at $9 on the live account from Tests/config.json and cancels it at the end.")]
    public void LiveLimitOrderStaysWithLeanWhileOrderPlacedOutsideLeanIsNotifiedOnce()
    {
        using var brokerage = new TestableTastytradeBrokerage();
        using var liveOrderSubmitted = new ManualResetEventSlim(false);
        using var liveOrderCanceled = new ManualResetEventSlim(false);

        // NOK trades above $10, so a buy limit at $9 stays open until the cancel.
        var liveOrder = new LimitOrder(Symbol.Create("NOK", SecurityType.Equity, Market.USA), 1m, 9m, DateTime.UtcNow);
        brokerage.OrderProvider.Add(liveOrder);

        brokerage.OrdersStatusChanged += (_, events) =>
        {
            foreach (var orderEvent in events)
            {
                if (orderEvent.OrderId != liveOrder.Id)
                {
                    continue;
                }

                switch (orderEvent.Status)
                {
                    case OrderStatus.Submitted:
                        liveOrderSubmitted.Set();
                        break;
                    case OrderStatus.Canceled:
                        liveOrderCanceled.Set();
                        break;
                }
            }
        };

        brokerage.Connect();
        try
        {
            Assert.That(brokerage.PlaceOrder(liveOrder), Is.True, "Live order: not placed.");
            Assert.That(liveOrderSubmitted.Wait(TimeSpan.FromSeconds(30)), Is.True, "Live order: no Submitted event in 30 s.");

            foreach (var message in CapturedLimitOrderMessages)
            {
                brokerage.ReceiveAccountStreamMessage(message);
            }
        }
        finally
        {
            // The order is real, so it is cancelled even when a step above fails.
            if (liveOrder.BrokerId.Count > 0)
            {
                brokerage.CancelOrder(liveOrder);
                liveOrderCanceled.Wait(TimeSpan.FromSeconds(30));
            }
            brokerage.Disconnect();
        }

        // Assert: brokerageSideOrder
        var brokerageSideOrder = brokerage.OrderProvider.GetOrdersByBrokerageId("507349840").Single();
        Assert.That(brokerageSideOrder.Type, Is.EqualTo(OrderType.Limit), "Brokerage side order: wrong type.");
        Assert.That(((LimitOrder)brokerageSideOrder).LimitPrice, Is.EqualTo(10.51m), "Brokerage side order: wrong limit price.");
        Assert.That(brokerageSideOrder.Quantity, Is.EqualTo(1m), "Brokerage side order: wrong quantity.");
        Assert.That(brokerageSideOrder.Symbol, Is.EqualTo(liveOrder.Symbol), "Brokerage side order: wrong symbol.");

        var brokerageSideOrderEvents = brokerage.OrderProvider.GetOrderTicket(brokerageSideOrder.Id).OrderEvents;
        Assert.That(brokerageSideOrderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.Submitted, OrderStatus.Filled }),
            "Brokerage side order: wrong order events.");
        Assert.That(brokerageSideOrderEvents[0].Message, Is.EqualTo("Order was submitted outside Lean"), "Brokerage side order: wrong Submitted message.");
        Assert.That(brokerageSideOrderEvents[1].FillQuantity, Is.EqualTo(1m), "Brokerage side order: wrong fill quantity.");
        Assert.That(brokerageSideOrderEvents[1].FillPrice, Is.EqualTo(10.505m), "Brokerage side order: wrong fill price.");
        // The fill of the brokerage side order is the only fill, the live order ends cancelled.
        Assert.That(brokerage.SecurityProvider.GetHoldingsQuantity(brokerageSideOrder.Symbol), Is.EqualTo(1m), "Holdings: wrong quantity.");

        // Assert: live order
        var liveOrderEvents = brokerage.OrderProvider.GetOrderTicket(liveOrder.Id).OrderEvents;
        Assert.That(liveOrderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.Submitted, OrderStatus.Canceled }),
            "Live order: not cancelled, see the 'Cancel Order' warning in the log.");
    }

    [Test]
    public void AcceptedOrderPlacedOutsideLeanIsSubmittedOnce()
    {
        using var brokerage = new TestableTastytradeBrokerage();

        // Routed notifies the algorithm about the order and reports it as submitted; Live then finds a known order and adds nothing.
        brokerage.ReceiveAccountStreamMessage(CapturedLimitOrderMessages[0]);
        brokerage.ReceiveAccountStreamMessage(CapturedLimitOrderMessages[1]);

        var brokerageSideOrder = brokerage.OrderProvider.GetOrdersByBrokerageId("507349840").Single();
        var orderEvents = brokerage.OrderProvider.GetOrderTicket(brokerageSideOrder.Id).OrderEvents;
        Assert.That(orderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.Submitted }), "Brokerage side order: wrong order events.");
        Assert.That(orderEvents[0].Message, Is.EqualTo("Order was submitted outside Lean"), "Brokerage side order: wrong Submitted message.");
    }

    [Test]
    public void DeclinedOrderPlacedOutsideLeanIsNotifiedOnce()
    {
        // Lean's default brokerage message handler declines the order, so it never gets a Lean id
        // and every later update of it reaches the brokerage as an unknown order again.
        using var brokerage = new TestableTastytradeBrokerage { AcceptBrokerageSideOrders = false };
        var notifications = 0;
        brokerage.NewBrokerageOrderNotification += (_, _) => notifications++;

        foreach (var message in CapturedLimitOrderMessages)
        {
            brokerage.ReceiveAccountStreamMessage(message);
        }

        Assert.That(notifications, Is.EqualTo(1), "Brokerage side order: not notified exactly once.");
        Assert.That(brokerage.OrderProvider.GetOrdersByBrokerageId("507349840"), Is.Empty, "Brokerage side order: tracked after the decline.");
    }

    [Test]
    public void OrderSentByLeanIsNotNotified()
    {
        // The cancel of the old brokerage id of a NOK Limit order that Lean replaced through UpdateOrder. Lean has already moved
        // its order to the new id, so it does not know this one, but the source says Lean sent it.
        // Captured from the live account stream on 2026-09-17; only the account number is masked.
        const string replacedOrderCancelledMessage =
            """{"type":"Order","data":{"id":507341323,"account-number":"5WY00000","cancellable":false,"cancelled-at":"2026-09-17T18:15:12.771+00:00","cancelled-size":"1.0","editable":false,"edited":true,"ext-client-order-id":"JAAAC1DMLFYBQ1ikZS","global-request-id":"557a4da46ce078bd97aa9cd37166863b","leg-count":1,"order-type":"Limit","price":"10.0","price-effect":"Debit","received-at":"2026-09-17T18:15:12.227+00:00","replacing-order-id":507341331,"size":1,"source":"QuantConnect","status":"Cancelled","terminal-at":"2026-09-17T18:15:12.787+00:00","time-in-force":"GTC","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789668912800,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"timestamp":1789668912805,"ws-sequence":3}""";

        using var brokerage = new TestableTastytradeBrokerage();
        var notifications = 0;
        brokerage.NewBrokerageOrderNotification += (_, _) => notifications++;

        brokerage.ReceiveAccountStreamMessage(replacedOrderCancelledMessage);

        Assert.That(notifications, Is.EqualTo(0), "Replaced order: notified as an order placed outside Lean.");
        Assert.That(brokerage.OrderProvider.GetOrdersByBrokerageId("507341323"), Is.Empty, "Replaced order: tracked as a new order.");
    }

    [Test]
    public void LeanOrderEditedInTheAppIsCanceledAndItsReplacementIsNotified()
    {
        // A NOK Limit order that Lean placed at $10.29 and the user then changed to $10.25 in the Tastytrade web app. Tastytrade never edits
        // an order in place: it cancels the old id, then the replacement arrives as a new id with the source of the app.
        // Captured from the live account stream on 2026-09-17, in the order the socket sent them; only the account number is masked.
        const string oldIdCancelledMessage =
            """{"type":"Order","data":{"id":507358650,"account-number":"5WY00000","cancellable":false,"cancelled-at":"2026-09-17T18:52:28.046+00:00","cancelled-size":"1.0","editable":false,"edited":true,"ext-client-order-id":"JAAAC1DjaMXU2XYwWC","global-request-id":"6b561ad90ef9c93d6811049d56720fed","leg-count":1,"order-type":"Limit","price":"10.29","price-effect":"Debit","received-at":"2026-09-17T18:51:59.992+00:00","replacing-order-id":507358867,"size":1,"source":"QuantConnect","status":"Cancelled","terminal-at":"2026-09-17T18:52:28.065+00:00","time-in-force":"GTC","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789671148078,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"timestamp":1789671148084,"ws-sequence":7}""";
        const string newIdRoutedMessage =
            """{"type":"Order","data":{"id":507358867,"account-number":"5WY00000","cancellable":false,"editable":false,"edited":false,"global-request-id":"64d60e84189f87698ba75276fd8a3952","leg-count":1,"order-type":"Limit","price":"10.25","price-effect":"Debit","received-at":"2026-09-17T18:52:28.016+00:00","replaces-order-id":507358650,"size":1,"source":"WB2;0.174.2","status":"Routed","time-in-force":"GTC","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789671148184,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"timestamp":1789671148195,"ws-sequence":8}""";
        const string newIdLiveMessage =
            """{"type":"Order","data":{"id":507358867,"account-number":"5WY00000","cancellable":true,"editable":true,"edited":false,"ext-client-order-id":"JAAAC1DjsPWqZlw6Nc","global-request-id":"64d60e84189f87698ba75276fd8a3952","leg-count":1,"order-type":"Limit","price":"10.25","price-effect":"Debit","received-at":"2026-09-17T18:52:28.016+00:00","replaces-order-id":507358650,"size":1,"source":"WB2;0.174.2","status":"Live","time-in-force":"GTC","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789671148235,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"timestamp":1789671148241,"ws-sequence":9}""";

        using var brokerage = new TestableTastytradeBrokerage();
        var messages = new List<BrokerageMessageEvent>();
        brokerage.Message += (_, message) => messages.Add(message);

        var leanOrder = new LimitOrder(Symbol.Create("NOK", SecurityType.Equity, Market.USA), 1m, 10.29m, new DateTime(2026, 9, 17, 18, 51, 59, DateTimeKind.Utc));
        leanOrder.BrokerId.Add("507358650");
        brokerage.OrderProvider.Add(leanOrder);

        brokerage.ReceiveAccountStreamMessage(oldIdCancelledMessage);
        brokerage.ReceiveAccountStreamMessage(newIdRoutedMessage);
        brokerage.ReceiveAccountStreamMessage(newIdLiveMessage);

        // Assert: the order Lean placed
        var leanOrderEvents = brokerage.OrderProvider.GetOrderTicket(leanOrder.Id).OrderEvents;
        Assert.That(leanOrderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.Canceled }), "Lean order: wrong order events.");

        // Assert: its replacement
        var replacementOrder = brokerage.OrderProvider.GetOrdersByBrokerageId("507358867").Single();
        Assert.That(((LimitOrder)replacementOrder).LimitPrice, Is.EqualTo(10.25m), "Replacement order: wrong limit price.");
        Assert.That(replacementOrder.Quantity, Is.EqualTo(1m), "Replacement order: wrong quantity.");

        var replacementOrderEvents = brokerage.OrderProvider.GetOrderTicket(replacementOrder.Id).OrderEvents;
        Assert.That(replacementOrderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.Submitted }), "Replacement order: wrong order events.");
        Assert.That(replacementOrderEvents[0].Message, Is.EqualTo("Order was submitted outside Lean"), "Replacement order: wrong Submitted message.");

        // Assert: the warning that links the two orders
        var warning = messages.Single();
        Assert.That(warning.Type, Is.EqualTo(BrokerageMessageType.Warning), "Warning: wrong message type.");
        Assert.That(warning.Code, Is.EqualTo("OrderEditedOutsideLean"), "Warning: wrong code.");
        Assert.That(warning.Message, Is.EqualTo($"OrderID {leanOrder.Id} was edited outside of the algorithm: Tastytrade cancelled it and created brokerage order 507358867 in its place."), "Warning: wrong text.");
    }

    [Test]
    public void OrderPlacedInTheAppAndReplacedByLeanIsNotNotified()
    {
        // A NOK Limit order at $9 that was open in the Tastytrade web app before the algorithm started, so Lean got it from GetOpenOrders.
        // The algorithm then changed it to $10.6 through UpdateOrder. Tastytrade cancels the old id and creates a new one: the new id carries
        // the source of Lean, but the old id keeps the source of the app that placed it.
        // Everything below was captured from the live account on 2026-09-18; only the account number is masked.
        // The REST answer to GetOpenOrders and the REST answer to the replace request of UpdateOrder:
        const string openOrdersResponse =
            """{"data":{"items":[{"id":507440771,"account-number":"5WY00000","cancellable":true,"editable":true,"edited":false,"ext-client-order-id":"JAAAC1FVkURM4osB98","global-request-id":"1aa952a7d4965a0fee581cb543dde473","leg-count":1,"order-type":"Limit","price":"9.0","price-effect":"Debit","received-at":"2026-09-18T13:09:35.885+00:00","size":1,"source":"WB2;0.175.0","status":"Live","time-in-force":"Day","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789736975953,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]}]},"context":"/accounts/5WY00000/orders","pagination":{"per-page":200,"page-offset":0,"item-offset":0,"total-items":1,"total-pages":1,"current-item-count":1,"previous-link":null,"next-link":null,"paging-link-template":null}}""";
        const string replaceOrderResponse =
            """{"data":{"id":507440943,"account-number":"5WY00000","cancellable":true,"contingent-status":"Pending Order","editable":true,"edited":false,"global-request-id":"5e9e828e7a5d44c05d4ae8bf332a82b9","leg-count":1,"order-type":"Limit","price":"10.6","price-effect":"Debit","received-at":"2026-09-18T13:11:00.899+00:00","replaces-order-id":507440771,"size":1,"source":"QuantConnect","status":"Contingent","time-in-force":"Day","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789737060899,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"context":"/accounts/5WY00000/orders/507440771"}""";
        // The account stream messages, in the order the socket sent them:
        const string oldIdCancelledMessage =
            """{"type":"Order","data":{"id":507440771,"account-number":"5WY00000","cancellable":false,"cancelled-at":"2026-09-18T13:11:00.928+00:00","cancelled-size":"1.0","editable":false,"edited":true,"ext-client-order-id":"JAAAC1FVkURM4osB98","global-request-id":"1aa952a7d4965a0fee581cb543dde473","leg-count":1,"order-type":"Limit","price":"9.0","price-effect":"Debit","received-at":"2026-09-18T13:09:35.885+00:00","replacing-order-id":507440943,"size":1,"source":"WB2;0.175.0","status":"Cancelled","terminal-at":"2026-09-18T13:11:00.943+00:00","time-in-force":"Day","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789737060955,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"timestamp":1789737060960,"ws-sequence":4}""";
        const string newIdRoutedMessage =
            """{"type":"Order","data":{"id":507440943,"account-number":"5WY00000","cancellable":false,"editable":false,"edited":false,"global-request-id":"5e9e828e7a5d44c05d4ae8bf332a82b9","leg-count":1,"order-type":"Limit","price":"10.6","price-effect":"Debit","received-at":"2026-09-18T13:11:00.899+00:00","replaces-order-id":507440771,"size":1,"source":"QuantConnect","status":"Routed","time-in-force":"Day","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789737061042,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"timestamp":1789737061052,"ws-sequence":5}""";
        const string newIdLiveMessage =
            """{"type":"Order","data":{"id":507440943,"account-number":"5WY00000","cancellable":true,"editable":true,"edited":false,"ext-client-order-id":"JAAAC1FVynNh89zc7L","global-request-id":"5e9e828e7a5d44c05d4ae8bf332a82b9","leg-count":1,"order-type":"Limit","price":"10.6","price-effect":"Debit","received-at":"2026-09-18T13:11:00.899+00:00","replaces-order-id":507440771,"size":1,"source":"QuantConnect","status":"Live","time-in-force":"Day","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789737061093,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"timestamp":1789737061098,"ws-sequence":6}""";

        var httpHandler = new MockHttpMessageHandler();
        httpHandler.SetResponse(HttpMethod.Get, "/orders", openOrdersResponse);
        httpHandler.SetResponse(HttpMethod.Patch, "/orders/507440771", replaceOrderResponse);

        using var brokerage = new TestableTastytradeBrokerage(httpHandler: httpHandler);
        using var orderIdChanged = new ManualResetEventSlim(false);
        var notifications = 0;
        brokerage.NewBrokerageOrderNotification += (_, _) => notifications++;
        brokerage.OrderIdChanged += (_, _) => orderIdChanged.Set();

        // Lean reads the open orders at the start and gives each of them a Lean id.
        var leanOrder = brokerage.GetOpenOrders().Single();
        brokerage.OrderProvider.Add(leanOrder);

        // UpdateOrder waits for the new id on the account stream, so it runs aside while the messages arrive.
        leanOrder.ApplyUpdateOrderRequest(new UpdateOrderRequest(new DateTime(2026, 9, 18, 13, 11, 0, DateTimeKind.Utc), leanOrder.Id, new UpdateOrderFields { LimitPrice = 10.6m }));
        var updateOrder = Task.Run(() => brokerage.UpdateOrder(leanOrder));
        Assert.That(orderIdChanged.Wait(TimeSpan.FromSeconds(10)), Is.True, "Lean order: the replace request did not change the brokerage id.");

        brokerage.ReceiveAccountStreamMessage(oldIdCancelledMessage);
        brokerage.ReceiveAccountStreamMessage(newIdRoutedMessage);
        brokerage.ReceiveAccountStreamMessage(newIdLiveMessage);
        Assert.That(updateOrder.Wait(TimeSpan.FromSeconds(10)) && updateOrder.Result, Is.True, "Lean order: UpdateOrder did not finish.");

        // Assert: the old id is not an order placed outside Lean
        Assert.That(notifications, Is.EqualTo(0), "Replaced order: notified as an order placed outside Lean.");
        Assert.That(brokerage.OrderProvider.GetOrdersByBrokerageId("507440771"), Is.Empty, "Replaced order: tracked as a new order.");
        Assert.That(brokerage.OrderProvider.OrdersCount, Is.EqualTo(1), "Replaced order: Lean has more than its own order.");

        // Assert: the order Lean updated
        Assert.That(leanOrder.BrokerId, Is.EqualTo(new[] { "507440943" }), "Lean order: wrong brokerage id.");
        var leanOrderEvents = brokerage.OrderProvider.GetOrderTicket(leanOrder.Id).OrderEvents;
        Assert.That(leanOrderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.UpdateSubmitted }), "Lean order: wrong order events.");
    }
}
