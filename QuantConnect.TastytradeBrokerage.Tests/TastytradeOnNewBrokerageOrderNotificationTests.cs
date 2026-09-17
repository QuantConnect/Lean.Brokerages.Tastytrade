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
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Brokerages.Tastytrade.Tests.Models;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

/// <summary>
/// Orders placed in the account outside the algorithm reach the account stream like any other order.
/// The brokerage offers each of them once to the algorithm through <see cref="Brokerage.NewBrokerageOrderNotification"/>.
/// </summary>
[TestFixture]
public class TastytradeOnNewBrokerageOrderNotificationTests
{
    /// <summary>
    /// Account stream messages of one NOK Limit order at $11 that filled at $10.665, from Routed to Filled,
    /// in the order the socket sent them. The Live message already carries the fill.
    /// Captured from the live account stream on 2026-09-17; only the account number is masked.
    /// </summary>
    private static readonly string[] CapturedLimitOrderMessages =
    [
        // Routed
        """{"type":"Order","data":{"id":507112736,"account-number":"5WY00000","cancellable":false,"editable":false,"edited":false,"global-request-id":"f7a1805ee0ccb95aa5087ed3bfdbfbb0","leg-count":1,"order-type":"Limit","price":"11.0","price-effect":"Debit","received-at":"2026-09-17T13:33:43.413+00:00","size":1,"source":"QuantConnect","status":"Routed","time-in-force":"GTC","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789652023457,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"timestamp":1789652023555,"ws-sequence":1}""",
        // Live, the fill is already on the leg
        """{"type":"Order","data":{"id":507112736,"account-number":"5WY00000","cancellable":true,"editable":true,"edited":false,"ext-client-order-id":"JAAAC08PhKAFUSqX9Z","global-request-id":"f7a1805ee0ccb95aa5087ed3bfdbfbb0","leg-count":1,"order-type":"Limit","price":"11.0","price-effect":"Debit","received-at":"2026-09-17T13:33:43.413+00:00","size":1,"source":"QuantConnect","status":"Live","time-in-force":"GTC","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789652023617,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":0,"symbol":"NOK","fills":[{"destination-venue":"HUDSON_RIVER_TRADING_A","fill-id":"3686196307888014350","fill-price":"10.665","filled-at":"2026-09-17T13:33:43.567+00:00","quantity":1}]}]},"timestamp":1789652023762,"ws-sequence":2}""",
        // Filled
        """{"type":"Order","data":{"id":507112736,"account-number":"5WY00000","cancellable":false,"editable":false,"edited":false,"ext-client-order-id":"JAAAC08PhKAFUSqX9Z","global-request-id":"f7a1805ee0ccb95aa5087ed3bfdbfbb0","leg-count":1,"order-type":"Limit","price":"11.0","price-effect":"Debit","received-at":"2026-09-17T13:33:43.413+00:00","size":1,"source":"QuantConnect","status":"Filled","terminal-at":"2026-09-17T13:33:43.757+00:00","time-in-force":"GTC","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789652023763,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":0,"symbol":"NOK","fills":[{"destination-venue":"HUDSON_RIVER_TRADING_A","fill-id":"3686196307888014350","fill-price":"10.665","filled-at":"2026-09-17T13:33:43.567+00:00","quantity":1}]}]},"timestamp":1789652023835,"ws-sequence":5}"""
    ];

    [Test, Explicit("Places a real NOK Limit order at $9 on the live account from Tests/config.json and cancels it at the end.")]
    public void LiveLimitOrderStaysWithLeanWhileOrderPlacedOutsideLeanIsOfferedOnce()
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
        var brokerageSideOrder = brokerage.OrderProvider.GetOrdersByBrokerageId("507112736").Single();
        Assert.That(brokerageSideOrder.Type, Is.EqualTo(OrderType.Limit), "Brokerage side order: wrong type.");
        Assert.That(((LimitOrder)brokerageSideOrder).LimitPrice, Is.EqualTo(11m), "Brokerage side order: wrong limit price.");
        Assert.That(brokerageSideOrder.Quantity, Is.EqualTo(1m), "Brokerage side order: wrong quantity.");
        Assert.That(brokerageSideOrder.Symbol, Is.EqualTo(liveOrder.Symbol), "Brokerage side order: wrong symbol.");

        var brokerageSideOrderEvents = brokerage.OrderProvider.GetOrderTicket(brokerageSideOrder.Id).OrderEvents;
        Assert.That(brokerageSideOrderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.Submitted, OrderStatus.Filled }),
            "Brokerage side order: wrong order events.");
        Assert.That(brokerageSideOrderEvents[0].Message, Is.EqualTo("Order was submitted outside Lean"), "Brokerage side order: wrong Submitted message.");
        Assert.That(brokerageSideOrderEvents[1].FillQuantity, Is.EqualTo(1m), "Brokerage side order: wrong fill quantity.");
        Assert.That(brokerageSideOrderEvents[1].FillPrice, Is.EqualTo(10.665m), "Brokerage side order: wrong fill price.");
        // The fill of the brokerage side order is the only fill, the live order ends cancelled.
        Assert.That(brokerage.SecurityProvider.GetHoldingsQuantity(brokerageSideOrder.Symbol), Is.EqualTo(1m), "Holdings: wrong quantity.");

        // Assert: live order
        var liveOrderEvents = brokerage.OrderProvider.GetOrderTicket(liveOrder.Id).OrderEvents;
        Assert.That(liveOrderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.Submitted, OrderStatus.Canceled }),
            "Live order: not cancelled, see the 'Cancel Order' warning in the log.");
    }
}
