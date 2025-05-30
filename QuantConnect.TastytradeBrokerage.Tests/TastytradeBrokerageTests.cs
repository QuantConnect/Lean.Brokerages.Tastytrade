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
using QuantConnect.Tests;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Tests.Brokerages;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture]
public partial class TastytradeBrokerageTests : BrokerageTests
{
    protected override Symbol Symbol => Symbols.AAPL;

    protected override SecurityType SecurityType => throw new NotImplementedException("This property must be overridden and should not be used directly.");

    protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
    {
        var baseUrl = Config.Get("tastytrade-api-url");
        var baseWSUrl = Config.Get("tastytrade-websocket-url");
        var username = Config.Get("tastytrade-username");
        var password = Config.Get("tastytrade-password");
        var accountNumber = Config.Get("tastytrade-account-number");

        return new TastytradeBrokerage(baseUrl, baseWSUrl, username, password, accountNumber, orderProvider, securityProvider);
    }

    protected override bool IsAsync() => false;

    protected override decimal GetAskPrice(Symbol symbol)
    {
        throw new NotImplementedException();
    }

    private static IEnumerable<OrderTestMetaData> OrderTestParameters
    {
        get
        {
            var aapl = Symbols.AAPL;
            yield return new OrderTestMetaData(OrderType.Market, aapl);
            yield return new OrderTestMetaData(OrderType.Limit, aapl, 4m, 2m);

            var option = Symbol.CreateOption(aapl, aapl.ID.Market, SecurityType.Option.DefaultOptionStyle(), OptionRight.Call, 200m, new DateTime(2025, 06, 20));
            yield return new OrderTestMetaData(OrderType.Market, option);
            yield return new OrderTestMetaData(OrderType.Limit, option, 4m, 2m);

            var index = Symbol.Create("SPX", SecurityType.Index, Market.USA);
            var indexOption = Symbol.CreateOption(index, Market.USA, SecurityType.IndexOption.DefaultOptionStyle(), OptionRight.Call, 5635m, new DateTime(2025, 06, 20));
            yield return new OrderTestMetaData(OrderType.Market, indexOption);
            yield return new OrderTestMetaData(OrderType.Limit, indexOption, 4m, 2m);

            var SP500EMini = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2025, 06, 20));
            yield return new OrderTestMetaData(OrderType.Market, SP500EMini);
            yield return new OrderTestMetaData(OrderType.Limit, SP500EMini, 4m, 2m);
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
    public void CloseFromLong(OrderTestMetaData orderTestMetaData)
    {
        var parameters = GetOrderTestParameters(orderTestMetaData);
        base.CloseFromLong(parameters);
    }

    [Test, TestCaseSource(nameof(OrderTestParameters))]
    public void ShortFromZero(OrderTestMetaData orderTestMetaData)
    {
        var parameters = GetOrderTestParameters(orderTestMetaData);
        base.ShortFromZero(parameters);
    }

    [Test, TestCaseSource(nameof(OrderTestParameters))]
    public void CloseFromShort(OrderTestMetaData orderTestMetaData)
    {
        var parameters = GetOrderTestParameters(orderTestMetaData);
        base.CloseFromShort(parameters);
    }

    [Test, TestCaseSource(nameof(OrderTestParameters))]
    public void ShortFromLong(OrderTestMetaData orderTestMetaData)
    {
        var parameters = GetOrderTestParameters(orderTestMetaData);
        base.ShortFromLong(parameters);
    }

    [Test, TestCaseSource(nameof(OrderTestParameters))]
    public void LongFromShort(OrderTestMetaData orderTestMetaData)
    {
        var parameters = GetOrderTestParameters(orderTestMetaData);
        base.LongFromShort(parameters);
    }

    /// <summary>
    /// Represents the parameters required for testing an order, including order type, symbol, and price limits.
    /// </summary>
    /// <param name="OrderType">The type of order being tested (e.g., Market, Limit, Stop).</param>
    /// <param name="Symbol">The financial symbol for the order, such as a stock or option ticker.</param>
    /// <param name="HighLimit">The high limit price for the order (if applicable).</param>
    /// <param name="LowLimit">The low limit price for the order (if applicable).</param>
    public record OrderTestMetaData(OrderType OrderType, Symbol Symbol, decimal HighLimit = 0, decimal LowLimit = 0);

    private static OrderTestParameters GetOrderTestParameters(OrderTestMetaData orderTestMetaData)
    {
        return GetOrderTestParameters(orderTestMetaData.OrderType, orderTestMetaData.Symbol, orderTestMetaData.HighLimit, orderTestMetaData.LowLimit);
    }

    private static OrderTestParameters GetOrderTestParameters(OrderType orderType, Symbol symbol, decimal highLimit, decimal lowLimit)
    {
        return orderType switch
        {
            OrderType.Market => new MarketOrderTestParameters(symbol),
            OrderType.Limit => new LimitOrderTestParameters(symbol, highLimit, lowLimit),
            OrderType.StopMarket => new StopMarketOrderTestParameters(symbol, highLimit, lowLimit),
            OrderType.StopLimit => new StopLimitOrderTestParameters(symbol, highLimit, lowLimit),
            _ => throw new NotImplementedException()
        };
    }
}