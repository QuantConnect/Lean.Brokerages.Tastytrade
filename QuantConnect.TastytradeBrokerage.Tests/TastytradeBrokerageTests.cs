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
using NUnit.Framework;
using System.Threading;
using QuantConnect.Tests;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Orders.TimeInForces;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture]
public partial class TastytradeBrokerageTests : BrokerageTests
{
    protected override Symbol Symbol => Symbols.AAPL;

    protected override SecurityType SecurityType => throw new NotImplementedException("This property must be overridden and should not be used directly.");

    protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
    {
        return TestSetup.CreateBrokerage(orderProvider, securityProvider);
    }

    protected override bool IsAsync() => false;

    protected override decimal GetAskPrice(Symbol symbol)
    {
        // In the sandbox environment, limit orders priced at $3 or below are filled immediately.
        // We return $2 here to ensure the order is filled during testing.
        return 2m;
    }

    /// <summary>
    /// Gets a collection of test parameters used for validating various order types in a simulated environment.
    /// </summary>
    /// <remarks>
    /// <b>Sandbox Behavior Rules:</b>
    /// <list type="bullet">
    /// <item>
    /// <description>Market orders will always fill at a price of <c>$1</c>.</description>
    /// </item>
    /// <item>
    /// <description>Limit orders with a price less than or equal to <c>$3</c> will fill immediately.</description>
    /// </item>
    /// <item>
    /// <description>Limit orders with a price greater than or equal to <c>$3</c> will be marked as <c>Live</c> and will not fill.</description>
    /// </item>
    /// </list>
    /// This collection includes test cases for equities, options, and index options. 
    /// Futures are excluded due to unsupported sandbox behavior (see commented section).
    /// </remarks>
    private static IEnumerable<OrderTestMetaData> OrderTestParameters
    {
        get
        {
            var aapl = Symbols.AAPL;
            yield return new OrderTestMetaData(OrderType.Market, aapl);
            yield return new OrderTestMetaData(OrderType.Limit, aapl, 2m, 4m);
            yield return new OrderTestMetaData(OrderType.Limit, aapl, 2m, 4m, new OrderProperties() { TimeInForce = new GoodTilDateTimeInForce(new DateTime(2025, 06, 20)) });
            yield return new OrderTestMetaData(OrderType.StopMarket, aapl, 2m, 3m);
            yield return new OrderTestMetaData(OrderType.StopLimit, aapl, 2m, 4m);

            var option = Symbol.CreateOption(aapl, aapl.ID.Market, SecurityType.Option.DefaultOptionStyle(), OptionRight.Call, 200m, new DateTime(2025, 06, 20));
            yield return new OrderTestMetaData(OrderType.Market, option);
            yield return new OrderTestMetaData(OrderType.Limit, option, 2m, 4m);
            yield return new OrderTestMetaData(OrderType.StopMarket, option, 2m, 3m);
            yield return new OrderTestMetaData(OrderType.StopLimit, option, 2m, 4m);

            var index = Symbol.Create("SPX", SecurityType.Index, Market.USA);
            var indexOption = Symbol.CreateOption(index, Market.USA, SecurityType.IndexOption.DefaultOptionStyle(), OptionRight.Call, 5635m, new DateTime(2025, 06, 20));
            yield return new OrderTestMetaData(OrderType.Market, indexOption);
            yield return new OrderTestMetaData(OrderType.Limit, indexOption, 4m, 4m);
            yield return new OrderTestMetaData(OrderType.StopMarket, indexOption, 2m, 3m);
            yield return new OrderTestMetaData(OrderType.StopLimit, indexOption, 2m, 4m);

            // Future. TODO: It doesn't work in SandBox
            //var SP500EMini = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2025, 06, 20));
            //yield return new OrderTestMetaData(OrderType.Market, SP500EMini);
            //yield return new OrderTestMetaData(OrderType.Limit, SP500EMini, 4m, 2m);
        }
    }

    [Test, TestCaseSource(nameof(OrderTestParameters))]
    public void CancelOrders(OrderTestMetaData orderTestMetaData)
    {
        var parameters = GetOrderTestParameters(orderTestMetaData);

        base.CancelOrders(parameters);
    }

    [Test, TestCaseSource(nameof(OrderTestParameters))]
    public void LongFromZero(OrderTestMetaData orderTestMetaData)
    {
        var parameters = GetOrderTestParameters(orderTestMetaData);
        base.LongFromZero(parameters);
    }

    [Test, TestCaseSource(nameof(OrderTestParameters))]
    public void ShortFromZero(OrderTestMetaData orderTestMetaData)
    {
        var parameters = GetOrderTestParameters(orderTestMetaData);
        base.ShortFromZero(parameters);
    }

    [Test, TestCaseSource(nameof(OrderTestParameters))]
    public void CloseFromLong(OrderTestMetaData orderTestMetaData)
    {
        var parameters = GetOrderTestParameters(orderTestMetaData);
        base.CloseFromLong(parameters);
    }

    [Test, TestCaseSource(nameof(OrderTestParameters))]
    public void CloseFromShort(OrderTestMetaData orderTestMetaData)
    {
        var parameters = GetOrderTestParameters(orderTestMetaData);
        base.CloseFromShort(parameters);
    }

    private static IEnumerable<OrderTestMetaData> CrossZeroOrderTestParameters
    {
        get
        {
            var aapl = Symbols.AAPL;
            yield return new OrderTestMetaData(OrderType.Market, aapl);
            yield return new OrderTestMetaData(OrderType.Limit, aapl, 2m, 2.9m);
            yield return new OrderTestMetaData(OrderType.Limit, aapl, 2m, 2.9m, new OrderProperties() { TimeInForce = new GoodTilDateTimeInForce(new DateTime(2025, 06, 20)) });
            yield return new OrderTestMetaData(OrderType.StopLimit, aapl, 2m, 2.9m);

            var option = Symbol.CreateOption(aapl, aapl.ID.Market, SecurityType.Option.DefaultOptionStyle(), OptionRight.Call, 200m, new DateTime(2025, 06, 20));
            yield return new OrderTestMetaData(OrderType.Market, option);
            yield return new OrderTestMetaData(OrderType.Limit, option, 2m, 2.9m);
            yield return new OrderTestMetaData(OrderType.StopLimit, option, 2m, 2.9m);

            var index = Symbol.Create("SPX", SecurityType.Index, Market.USA);
            var indexOption = Symbol.CreateOption(index, Market.USA, SecurityType.IndexOption.DefaultOptionStyle(), OptionRight.Call, 5635m, new DateTime(2025, 06, 20));
            yield return new OrderTestMetaData(OrderType.Market, indexOption);
            yield return new OrderTestMetaData(OrderType.Limit, indexOption, 4m, 2.9m);
            yield return new OrderTestMetaData(OrderType.StopLimit, indexOption, 2m, 2.9m);
        }
    }

    [Test, TestCaseSource(nameof(CrossZeroOrderTestParameters))]
    public void ShortFromLong(OrderTestMetaData orderTestMetaData)
    {
        var parameters = GetOrderTestParameters(orderTestMetaData);
        base.ShortFromLong(parameters);
    }

    [Test, TestCaseSource(nameof(CrossZeroOrderTestParameters))]
    public void LongFromShort(OrderTestMetaData orderTestMetaData)
    {
        var parameters = GetOrderTestParameters(orderTestMetaData);
        base.LongFromShort(parameters);
    }

    [Test]
    public void LongFromZeroAndUpdate()
    {
        var orderTestMetaData = new OrderTestMetaData(OrderType.Limit, Symbols.AAPL, 2m, 4m);
        var parameters = GetOrderTestParameters(orderTestMetaData);

        var order = PlaceOrderWaitForStatus(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus) as LimitOrder;

        var updateOrderRequest = new UpdateOrderRequest(DateTime.UtcNow, order.Id, new()
        {
            LimitPrice = order.LimitPrice - 0.5m,
        });

        order.ApplyUpdateOrderRequest(updateOrderRequest);

        using var canceledOrderStatusEvent = new AutoResetEvent(false);
        using var updatedOrderStatusEvent = new AutoResetEvent(false);
        Brokerage.OrdersStatusChanged += (_, orderEvents) =>
        {
            var eventOrderStatus = orderEvents[0].Status;

            order.Status = eventOrderStatus;

            switch (eventOrderStatus)
            {
                case OrderStatus.UpdateSubmitted:
                    updatedOrderStatusEvent.Set();
                    break;
                case OrderStatus.Canceled:
                    canceledOrderStatusEvent.Set();
                    break;
            }
        };

        if (!Brokerage.UpdateOrder(order))
        {
            Assert.Fail("Order is updated well.");
        }

        Assert.IsTrue(updatedOrderStatusEvent.WaitOne(TimeSpan.FromSeconds(10)));

        if (!Brokerage.CancelOrder(order) || !canceledOrderStatusEvent.WaitOne(TimeSpan.FromSeconds(5)))
        {
            Assert.Fail("Order is not canceled well.");
        }
    }

    /// <summary>
    /// Represents the parameters required for testing an order, including order type, symbol, and price limits.
    /// </summary>
    /// <param name="OrderType">The type of order being tested (e.g., Market, Limit, Stop).</param>
    /// <param name="Symbol">The financial symbol for the order, such as a stock or option ticker.</param>
    /// <param name="HighLimit">The high limit price for the order (if applicable).</param>
    /// <param name="LowLimit">The low limit price for the order (if applicable).</param>
    public record OrderTestMetaData(OrderType OrderType, Symbol Symbol, decimal HighLimit = 0, decimal LowLimit = 0, IOrderProperties OrderProperties = default);

    private static OrderTestParameters GetOrderTestParameters(OrderTestMetaData orderTestMetaData)
    {
        return GetOrderTestParameters(orderTestMetaData.OrderType, orderTestMetaData.Symbol, orderTestMetaData.HighLimit, orderTestMetaData.LowLimit, orderTestMetaData.OrderProperties);
    }

    private static OrderTestParameters GetOrderTestParameters(OrderType orderType, Symbol symbol, decimal highLimit, decimal lowLimit, IOrderProperties orderProperties)
    {
        return orderType switch
        {
            OrderType.Market => new MarketOrderTestParameters(symbol, orderProperties),
            OrderType.Limit => new LimitOrderTestParameters(symbol, highLimit, lowLimit, orderProperties),
            OrderType.StopMarket => new StopMarketOrderTestParameters(symbol, highLimit, lowLimit, orderProperties),
            OrderType.StopLimit => new StopLimitOrderTestParameters(symbol, highLimit, lowLimit, orderProperties),
            _ => throw new NotImplementedException()
        };
    }
}