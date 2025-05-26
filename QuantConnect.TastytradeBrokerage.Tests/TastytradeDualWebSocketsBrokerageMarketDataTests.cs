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
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.WebSocket;
using QuantConnect.Brokerages.Tastytrade.Models.Stream;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture]
public class TastytradeDualWebSocketsBrokerageMarketDataTests
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
            authenticateResetEvent.Set();
        };
        testDualWebSocketsBrokerage.ExceptionResponse += (_, exception) =>
        {
            assertErrorMessage = exception.Message;
            cancellationTokenSource.Cancel();
        };

        testDualWebSocketsBrokerage.Connect();

        Assert.IsTrue(authenticateResetEvent.WaitOne(TimeSpan.FromSeconds(200), cancellationTokenSource.Token), assertErrorMessage);
    }

    [Test]
    public void ConnectShouldReceiveKeepAliveSeveralTimes()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var keepAliveResetEvent = new AutoResetEvent(false);
        using var testDualWebSocketsBrokerage = new TestDualWebSocketsBrokerage("Tastytrade");
        var assertErrorMessage = "Authentication did not complete successfully within the timeout.";

        var keepAliveCounter = 0;
        testDualWebSocketsBrokerage.KeepAliveResponse += (_, _) =>
        {
            if (keepAliveCounter++ >= 3)
            {
                keepAliveResetEvent.Set();
            }
        };

        testDualWebSocketsBrokerage.ExceptionResponse += (_, exception) =>
        {
            assertErrorMessage = exception.Message;
            cancellationTokenSource.Cancel();
        };

        testDualWebSocketsBrokerage.Connect();

        Assert.IsTrue(keepAliveResetEvent.WaitOne(TimeSpan.FromSeconds(200), cancellationTokenSource.Token), assertErrorMessage);
    }

    private static IEnumerable<TestCaseData> TestParameters
    {
        get
        {
            yield return new TestCaseData(new List<string> { "AAPL", "AMZN", "TSLA", "ITNL", "NVDA", "PLTR" });

        }
    }

    [Test, TestCaseSource(nameof(TestParameters))]
    public void SubscribeOnTickers(List<string> tickers)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var authenticateResetEvent = new AutoResetEvent(false);
        using var marketDataReceiveResetEvent = new AutoResetEvent(false);
        using var testDualWebSocketsBrokerage = new TestDualWebSocketsBrokerage("Tastytrade");

        testDualWebSocketsBrokerage.ExceptionResponse += (_, exception) =>
        {
            cancellationTokenSource.Cancel();
        };

        testDualWebSocketsBrokerage.AuthenticationResponse += (_, authenticationResponse) =>
        {
            authenticateResetEvent.Set();
        };

        var marketDataReceiveCounter = 0;
        testDualWebSocketsBrokerage.MarketDataResponse += (_, message) =>
        {
            if (marketDataReceiveCounter++ >= 50)
            {
                marketDataReceiveResetEvent.Set();
            }
        };

        testDualWebSocketsBrokerage.Connect();

        Assert.IsTrue(authenticateResetEvent.WaitOne(TimeSpan.FromSeconds(10), cancellationTokenSource.Token), "Authentication did not complete successfully within the timeout.");

        testDualWebSocketsBrokerage.SubscribeOnTickers(tickers);

        Assert.IsTrue(marketDataReceiveResetEvent.WaitOne(TimeSpan.FromSeconds(100), cancellationTokenSource.Token));
    }


    public sealed class TestDualWebSocketsBrokerage : DualWebSocketsBrokerage
    {
        public event EventHandler<string> AuthenticationResponse;

        public event EventHandler<Exception> ExceptionResponse;

        public event EventHandler KeepAliveResponse;

        public event EventHandler<string> MarketDataResponse;

        public override bool IsConnected => MarketDataUpdatesWebSocket?.IsOpen ?? false;

        public TestDualWebSocketsBrokerage(string name) : base(name)
        {
            InitializeMarketDataUpdates(TestSetup.CreateTastytradeApiClient());
            MarketDataUpdatesWebSocket.Error += HandleAccountUpdatesWebSocketError;
        }

        private void HandleAccountUpdatesWebSocketError(object sender, WebSocketError e)
        {
            ExceptionResponse?.Invoke(this, e.Exception);
        }

        protected override void OnMarketDataMessage(object sender, WebSocketMessage e)
        {
            switch (e.Data)
            {
                case WebSocketClientWrapper.TextMessage textMessage:
                    Log.Debug($"{nameof(TestDualWebSocketsBrokerage)}.OnMarketDataMessage.WebSocketMessage: {textMessage.Message}");

                    var connectResponse = textMessage.Message.DeserializeCamelCase<BaseResponse>();
                    switch (connectResponse.Type)
                    {
                        case EventType.FeedConfig:
                            AuthenticationResponse?.Invoke(this, textMessage.Message);
                            break;
                        case EventType.KeepAlive:
                            KeepAliveResponse?.Invoke(this, new());
                            break;
                        case EventType.FeedData:
                            MarketDataResponse?.Invoke(this, textMessage.Message);
                            break;
                    }

                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void SubscribeOnTickers(IEnumerable<string> brokerageTickers)
        {
            var msg = new FeedSubscription(brokerageTickers).ToJson();

            Log.Debug($"{nameof(TestDualWebSocketsBrokerage)}.{nameof(SubscribeOnTickers)}.Send.Msg: " + msg);

            MarketDataUpdatesWebSocket.Send(msg);
        }

        protected override void OnTradeReceived(TradeContent trade)
        {
            throw new NotImplementedException();
        }

        protected override void OnQuoteReceived(QuoteContent quote)
        {
            throw new NotImplementedException();
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        protected override bool Subscribe(IEnumerable<Symbol> symbols)
        {
            throw new NotImplementedException();
        }

        protected override void OnMessage(object sender, WebSocketMessage e)
        {
            throw new NotSupportedException();
        }

        #region Brokerage functionality

        public override bool CancelOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override List<Holding> GetAccountHoldings()
        {
            throw new NotImplementedException();
        }

        public override List<CashAmount> GetCashBalance()
        {
            throw new NotImplementedException();
        }

        public override List<Order> GetOpenOrders()
        {
            throw new NotImplementedException();
        }

        public override bool PlaceOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateOrder(Order order)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
