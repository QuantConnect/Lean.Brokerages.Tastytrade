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
using System.Threading;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.Tastytrade.Services;

/// <summary>
/// Handles synchronization for Lean orders using an <see cref="AutoResetEvent"/>.
/// </summary>
public class PendingOrderManager : IDisposable
{
    /// <summary>
    /// Gets the Lean order being managed.
    /// </summary>
    public Order LeanOrder { get; }

    /// <summary>
    /// Gets the target order status that should trigger an action (e.g., signaling).
    /// </summary>
    public OrderStatus InvokeOrderStatus { get; }

    /// <summary>
    /// Indicates whether the order submission event should be invoked.
    /// Used in <c>CrossZeroOrder</c> processing to skip triggering the event for the second part of a split order.
    /// </summary>
    public bool IsInvokeOrderEvent { get; set; } = true;

    /// <summary>
    /// Gets the synchronization event used for order processing coordination.
    /// </summary>
    public AutoResetEvent AutoResetEvent { get; } = new(false);

    /// <summary>
    /// Initializes a new instance of the <see cref="PendingOrderManager"/> class.
    /// </summary>
    /// <param name="leanOrder">The Lean order to manage.</param>
    /// <param name="invokeOrderStatus">The order status that, when triggered, will be used to signal processing completion or continuation.</param>
    public PendingOrderManager(Order leanOrder, OrderStatus invokeOrderStatus)
    {
        LeanOrder = leanOrder;
        InvokeOrderStatus = invokeOrderStatus;
    }

    /// <summary>
    /// Releases resources used by the <see cref="PendingOrderManager"/> instance.
    /// </summary>
    public void Dispose()
    {
        AutoResetEvent?.Dispose();
    }
}
