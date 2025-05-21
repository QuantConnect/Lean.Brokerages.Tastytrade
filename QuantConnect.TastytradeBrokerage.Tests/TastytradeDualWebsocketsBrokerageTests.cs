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

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture]
public class TastytradeDualWebSocketsBrokerageTests
{
    [Test]
    public void ConnectShouldAuthenticateWithinTimeout()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var authenticateResetEvent = new AutoResetEvent(false);
        using var testDualWebSocketsBrokerage = new TestDualWebSocketsBrokerage("Tastytrade");


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

        testDualWebSocketsBrokerage.Connect();

        Assert.IsTrue(authenticateResetEvent.WaitOne(TimeSpan.FromSeconds(100), cancellationTokenSource.Token), "Authentication did not complete successfully within the timeout.");
    }

    [Test]
    public void ConnectShouldReturnHeartbeatTwoTimesWithinTimeout()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var authenticateResetEvent = new AutoResetEvent(false);
        using var testDualWebSocketsBrokerage = new TestDualWebSocketsBrokerage("Tastytrade");

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

        testDualWebSocketsBrokerage.Connect();

        Assert.IsTrue(authenticateResetEvent.WaitOne(TimeSpan.FromSeconds(100), cancellationTokenSource.Token), "Authentication did not complete successfully within the timeout.");
    }

    public sealed class TestDualWebSocketsBrokerage : DualWebSocketsBrokerage
    {
        public event EventHandler<ConnectResponse> AuthenticationResponse;

        public event EventHandler<HeartbeatResponse> HeartbeatResponse;

        public TestDualWebSocketsBrokerage(string name) : base(name)
        {
            Initialize(Config.Get("tastytrade-websocket-url"), TestSetup.CreateTastytradeApiClient());
        }

        public override bool IsConnected => AccountUpdatesWebSocket?.IsOpen ?? false;

        public override void Connect()
        {
            base.Connect();
        }

        public override void Disconnect()
        {
            throw new System.NotImplementedException();
        }
        protected override void OnMessage(object sender, WebSocketMessage e)
        {
            switch (e.Data)
            {
                case WebSocketClientWrapper.TextMessage textMessage:
                    Log.Trace($"{nameof(TestDualWebSocketsBrokerage)}.OnMessage.WebSocketMessage: {textMessage.Message}");

                    var baseResponseMessage = textMessage.Message.DeserializeKebabCase<BaseResponseMessage>();

                    switch (baseResponseMessage.Action)
                    {
                        case ActionStream.Connect:
                            var connectResponse = textMessage.Message.DeserializeKebabCase<ConnectResponse>();
                            if (connectResponse.Status == Status.Ok)
                            {
                                AuthenticationResponse?.Invoke(this, connectResponse);
                            }
                            break;
                        case ActionStream.Heartbeat:
                            var heartbeatResponse = textMessage.Message.DeserializeKebabCase<HeartbeatResponse>();
                            if (heartbeatResponse.Status == Status.Ok)
                            {
                                HeartbeatResponse?.Invoke(this, heartbeatResponse);
                            }
                            break;
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }

        }

        protected override bool Subscribe(IEnumerable<Symbol> symbols) => throw new System.NotImplementedException();

        public override bool CancelOrder(Order order) => throw new System.NotImplementedException();

        public override List<Holding> GetAccountHoldings() => throw new System.NotImplementedException();

        public override List<CashAmount> GetCashBalance() => throw new System.NotImplementedException();

        public override List<Order> GetOpenOrders() => throw new System.NotImplementedException();

        public override bool PlaceOrder(Order order) => throw new System.NotImplementedException();

        public override bool UpdateOrder(Order order) => throw new System.NotImplementedException();
    }
}
