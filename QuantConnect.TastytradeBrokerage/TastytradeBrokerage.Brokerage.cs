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
using QuantConnect.Orders;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Orders;

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
    /// Gets all holdings for the account
    /// </summary>
    /// <returns>The current holdings from the account</returns>
    public override List<Holding> GetAccountHoldings()
    {
        var positions = _tastytradeApiClient.GetAccountPositions().SynchronouslyAwaitTaskResult();

        foreach (var position in positions)
        {

        }

        return [];
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
    public override List<Order> GetOpenOrders()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Places a new order and assigns a new broker ID to the order
    /// </summary>
    /// <param name="order">The order to be placed</param>
    /// <returns>True if the request for a new order has been placed, false otherwise</returns>
    public override bool PlaceOrder(Order order)
    {
        if (!CanSubscribe(order.Symbol))
        {
            return false;
        }

        var brokerageOrder = ConvertLeanOrderToBrokerageOrder(order);

        var response = _tastytradeApiClient.SubmitOrder(brokerageOrder);

        return true;
    }

    /// <summary>
    /// Updates the order with the same id
    /// </summary>
    /// <param name="order">The new order information</param>
    /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
    public override bool UpdateOrder(Order order)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Cancels the order with the specified ID
    /// </summary>
    /// <param name="order">The order to cancel</param>
    /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
    public override bool CancelOrder(Order order)
    {
        throw new NotImplementedException();
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
    private OrderBaseRequest ConvertLeanOrderToBrokerageOrder(Order order)
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
                brokerageOrder = new MarketOrderRequest(timeInForce, expiryDateTime, legs);
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
