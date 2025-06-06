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
using QuantConnect.Orders;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.WebSocket;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Stream;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;
using BrokerageOrder = QuantConnect.Brokerages.Tastytrade.Models.Orders.Order;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture]
public class TastytradeDualWebSocketsBrokerageAccountTests
{
    [Test]
    public void ConnectShouldAuthenticateWithinTimeout()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var authenticateResetEvent = new AutoResetEvent(false);
        using var testDualWebSocketsBrokerage = new TestDualWebSocketsBrokerage("Tastytrade");
        var assertErrorMessage = "Authentication did not complete successfully within the timeout.";

        testDualWebSocketsBrokerage.AuthenticationResponse += (_, authenticationResponse) =>
        {
            if (authenticationResponse.Status == Status.Ok)
            {
                authenticateResetEvent.Set();
            }
            else
            {
                cancellationTokenSource.Cancel();
            }
        };
        testDualWebSocketsBrokerage.ExceptionResponse += (_, exception) =>
        {
            assertErrorMessage = exception.Message;
            cancellationTokenSource.Cancel();
        };

        testDualWebSocketsBrokerage.Connect();

        Assert.IsTrue(authenticateResetEvent.WaitOne(TimeSpan.FromSeconds(100), cancellationTokenSource.Token), assertErrorMessage);
    }

    [Test]
    public void ConnectShouldReturnHeartbeatTwoTimesWithinTimeout()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var authenticateResetEvent = new AutoResetEvent(false);
        using var testDualWebSocketsBrokerage = new TestDualWebSocketsBrokerage("Tastytrade");
        var assertErrorMessage = "Heartbeat did not return responses successfully within the timeout.";

        var heartbeatCounterResponse = 0;
        testDualWebSocketsBrokerage.HeartbeatResponse += (_, HeartbeatResponse) =>
        {
            if (HeartbeatResponse.Status == Status.Ok)
            {
                if (heartbeatCounterResponse++ > 1)
                {
                    authenticateResetEvent.Set();
                }
            }
            else
            {
                cancellationTokenSource.Cancel();
            }
        };
        testDualWebSocketsBrokerage.ExceptionResponse += (_, exception) =>
        {
            assertErrorMessage = exception.Message;
            cancellationTokenSource.Cancel();
        };

        testDualWebSocketsBrokerage.Connect();

        Assert.IsTrue(authenticateResetEvent.WaitOne(TimeSpan.FromSeconds(100), cancellationTokenSource.Token), assertErrorMessage);
    }

    public sealed class TestDualWebSocketsBrokerage : DualWebSocketsBrokerage
    {
        public event EventHandler<ConnectResponse> AuthenticationResponse;

        public event EventHandler<HeartbeatResponse> HeartbeatResponse;

        public event EventHandler<Exception> ExceptionResponse;

        public TestDualWebSocketsBrokerage(string name) : base(name)
        {
            InitializeAccountUpdates(Config.Get("tastytrade-websocket-url"), TestSetup.CreateTastytradeApiClient());
            AccountUpdatesWebSocket.Error += HandleAccountUpdatesWebSocketError;
        }

        private void HandleAccountUpdatesWebSocketError(object sender, WebSocketError e)
        {
            ExceptionResponse?.Invoke(this, e.Exception);
        }

        public override bool IsConnected => AccountUpdatesWebSocket?.IsOpen ?? false;

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }
        protected override void OnMessageHandler(object sender, WebSocketMessage e)
        {
            switch (e.Data)
            {
                case WebSocketClientWrapper.TextMessage textMessage:
                    Log.Trace($"{nameof(TestDualWebSocketsBrokerage)}.OnMessage.WebSocketMessage: {textMessage.Message}");

                    var baseResponseMessage = textMessage.Message.DeserializeKebabCase<BaseAccountMaintenanceStatus>();

                    switch (baseResponseMessage.Action)
                    {
                        case ActionStream.Connect:
                            var connectResponse = textMessage.Message.DeserializeKebabCase<ConnectResponse>();
                            AuthenticationResponse?.Invoke(this, connectResponse);
                            break;
                        case ActionStream.Heartbeat:
                            var heartbeatResponse = textMessage.Message.DeserializeKebabCase<HeartbeatResponse>();
                            HeartbeatResponse?.Invoke(this, heartbeatResponse);
                            break;
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }

        }

        protected override bool Subscribe(IEnumerable<Symbol> symbols) => throw new NotImplementedException();

        protected override void OnOrderUpdateReceived(BrokerageOrder orderUpdates)
        {

        }

        public override bool CancelOrder(Order order) => throw new NotImplementedException();

        public override List<Holding> GetAccountHoldings() => throw new NotImplementedException();

        public override List<CashAmount> GetCashBalance() => throw new NotImplementedException();

        public override List<Order> GetOpenOrders() => throw new NotImplementedException();

        public override bool PlaceOrder(Order order) => throw new NotImplementedException();

        public override bool UpdateOrder(Order order) => throw new NotImplementedException();

        protected override void OnTradeReceived(TradeContent trade) => throw new NotImplementedException();

        protected override void OnQuoteReceived(QuoteContent quote) => throw new NotImplementedException();
    }
}
