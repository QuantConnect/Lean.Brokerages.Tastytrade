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
using QuantConnect.Api;
using System.Threading;
using QuantConnect.Util;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using System.Collections.Generic;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Logging;
using QuantConnect.Brokerages.Tastytrade.Api;
using QuantConnect.Brokerages.Tastytrade.WebSocket;
using QuantConnect.Brokerages.Authentication;
using Leg = QuantConnect.Brokerages.Tastytrade.Models.Orders.Leg;
using OrderAction = QuantConnect.Brokerages.Tastytrade.Models.Enum.OrderAction;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture]
public class TastytradeBrokerageAdditionalTests
{
    private Api.TastytradeApiClient _tastytradeApiClient;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _tastytradeApiClient = TestSetup.CreateTastytradeApiClient();
    }

    [Test]
    public void ParameterlessConstructorComposerUsage()
    {
        var brokerage = Composer.Instance.GetExportedValueByTypeName<IDataQueueHandler>(nameof(TastytradeBrokerage));
        Assert.IsNotNull(brokerage);
        Assert.IsInstanceOf<TastytradeBrokerage>(brokerage);
    }

    /// <summary>
    /// Reproduces the live outage where a subscription request coincided with the broker
    /// closing the market data WebSocket (e.g. Tastytrade's nightly maintenance window):
    /// the subscribe/unsubscribe feed requests were sent on a dead socket, threw, and the
    /// exception propagated through <see cref="Data.DataQueueHandlerSubscriptionManager"/>
    /// into the engine's live data feed, terminating the algorithm. Subscription requests
    /// must tolerate an unavailable connection — subscription state is tracked locally and
    /// re-synchronized with the broker on every (re)connect.
    /// </summary>
    [Test]
    public void SubscribeAndUnsubscribeDoNotThrowWhileMarketDataSocketIsNotConnected()
    {
        using var brokerage = TestSetup.CreateBrokerage(null, null);
        Assert.IsFalse(brokerage.IsConnected);

        var config = new Data.SubscriptionDataConfig(typeof(Data.Market.TradeBar), global::QuantConnect.Tests.Symbols.AAPL,
            Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, false, false, false);

        IEnumerator<Data.BaseData> enumerator = null;
        Assert.DoesNotThrow(() => enumerator = brokerage.Subscribe(config, (_, _) => { }));
        Assert.IsNotNull(enumerator);
        Assert.DoesNotThrow(() => brokerage.Unsubscribe(config));

        enumerator.Dispose();
    }

    /// <summary>
    /// Live soak: holds an idle DxLink connection and logs every inbound message with the gap since the previous one.
    /// The longest silence shows how often DxLink sends KEEPALIVE when idle (30s observed), which is what the
    /// 60s silence limit in <see cref="MarketDataWebSocketClientWrapper"/> relies on.
    /// </summary>
    [Test, Explicit("Requires valid Tastytrade credentials and holds a live DxLink connection idle for 5 minutes.")]
    public void MarketDataWebSocketWhenIdleForFiveMinutesStaysConnected()
    {
        // Arrange
        var openCount = 0;
        var errorCount = 0;
        var lastMessageUtc = DateTime.UtcNow;
        var longestGap = TimeSpan.Zero;
        var webSocket = new MarketDataWebSocketClientWrapper(_tastytradeApiClient, () => Log.Trace("IdleSoak: re-subscription requested"), (_, message) =>
        {
            var now = DateTime.UtcNow;
            var gap = now - lastMessageUtc;
            lastMessageUtc = now;
            if (gap > longestGap)
            {
                longestGap = gap;
            }
            // Log.Trace drops a line identical to the previous one; every idle KEEPALIVE line is identical.
            Log.Trace($"IdleSoak: received after {gap.TotalSeconds:F1}s: {((WebSocketClientWrapper.TextMessage)message.Data).Message}", overrideMessageFloodProtection: true);
        }, _ => { });
        webSocket.Open += (_, _) => Log.Trace($"IdleSoak: open #{Interlocked.Increment(ref openCount)}");
        webSocket.Error += (_, e) => Log.Trace($"IdleSoak: error #{Interlocked.Increment(ref errorCount)}: {e.Message}");
        webSocket.Closed += (_, _) => Log.Trace("IdleSoak: closed");

        try
        {
            // Act
            webSocket.Connect();
            Thread.Sleep(TimeSpan.FromMinutes(5));

            // Assert
            // The silence after the last message counts too; a server that goes quiet for good never produces another gap.
            var trailingSilence = DateTime.UtcNow - lastMessageUtc;
            if (trailingSilence > longestGap)
            {
                longestGap = trailingSilence;
            }
            Log.Trace($"IdleSoak: opens={openCount}, errors={errorCount}, longest silence={longestGap.TotalSeconds:F1}s, last message {trailingSilence.TotalSeconds:F1}s ago");
            Assert.That(webSocket.IsOpen, Is.True);
            Assert.That(openCount, Is.EqualTo(1), "The socket was opened again, so it was aborted at least once.");
            Assert.That(errorCount, Is.Zero);
            Assert.That(longestGap, Is.LessThan(TimeSpan.FromSeconds(60)), "DxLink stayed silent longer than the 60s the client promises in SETUP.");
        }
        finally
        {
            webSocket.Close();
        }
    }

    [Test]
    public void GetAccountBalances()
    {
        var res = _tastytradeApiClient.GetAccountBalances();

        Assert.IsNotNull(res);
        Assert.GreaterOrEqual(res.CashBalance, 0m);
        Assert.AreEqual(Currencies.USD, res.Currency);
        Assert.GreaterOrEqual(res.CashSettleBalance, 0m);
        Assert.GreaterOrEqual(res.AvailableTradingFunds, 0m);
    }

    [Test]
    public void GetAccountPositions()
    {
        var res = _tastytradeApiClient.GetAccountPositions();

        Assert.IsNotNull(res);
    }

    [Test]
    public void GetApiQuoteToken()
    {
        var res = _tastytradeApiClient.GetApiQuoteToken();

        Assert.IsNotNull(res);
        Assert.AreEqual("demo", res.Level);
    }

    [TestCase("ESM5", Description = "E-Mini S&P 500 Jun 25")]
    [TestCase("ESU5", Description = "E-Mini S&P 500 Jun 25")]
    [TestCase("GCZ5", Description = "Gold Dec 25")]
    [TestCase("6BZ5", Description = "British Pound Dec 25")]
    [TestCase("RBM5", Description = "RBOB Gasoline Jun 25")]
    [TestCase("MNQZ5", Description = "MicroNASDAQ100EMini Dec 25")]
    public void GetInstrumentFuture(string brokerageSymbol)
    {
        var res = _tastytradeApiClient.GetInstrumentFuture(brokerageSymbol);

        Assert.IsNotNull(res);
        Assert.IsNotNull(res.Symbol);
        Assert.IsNotEmpty(res.Symbol);
        Assert.IsNotNull(res.StreamerSymbol);
        Assert.IsNotEmpty(res.StreamerSymbol);
    }

    [TestCase("SPY", false)]
    [TestCase("SPX", true)]
    [TestCase("BRK/B", false)]
    public void IsUnderlyingEquityAnIndexAsync(string symbol, bool expectedIsIndex)
    {
        var actualIsIndex = _tastytradeApiClient.IsUnderlyingEquityAnIndexAsync(symbol);
        Assert.AreEqual(expectedIsIndex, actualIsIndex);
    }

    [Test]
    public void GetLiveOrders()
    {
        var res = _tastytradeApiClient.GetLiveOrders();

        Assert.IsNotNull(res);
    }

    [TestCase("123")]
    public void CancelOrderWithWrongId(string id)
    {
        Assert.Throws<Exception>(() => _tastytradeApiClient.CancelOrderById(id));
    }

    [TestCase("AAPL")]
    [TestCase("VIX")]
    public void GetOptionChains(string ticker)
    {
        var res = _tastytradeApiClient.GetOptionChains(ticker);

        Assert.IsNotNull(res);
    }

    [TestCase(Securities.Futures.Indices.SP500EMini)]
    [TestCase(Securities.Futures.Metals.Gold)]
    [TestCase(Securities.Futures.Financials.Y30TreasuryBond)]
    [TestCase(Securities.Futures.Indices.MicroNASDAQ100EMini)]
    public void GetFutureOptionChains(string ticker)
    {
        var res = _tastytradeApiClient.GetFutureOptionChains(ticker);

        Assert.IsNotNull(res);
    }

    [Test, Explicit("Requires valid refresh token for Tastytrade account.")]
    public void RefreshToken()
    {
        var baseUrl = Config.Get("tastytrade-api-url");
        var accountNumber = Config.Get("tastytrade-account-number");
        var refreshToken = Config.Get("tastytrade-refresh-token");

        var leanApiClient = new ApiConnection(Globals.UserId, Globals.UserToken);

        if (!leanApiClient.Connected)
        {
            throw new ArgumentException("Invalid api user id or token, cannot authenticate subscription.");
        }

        var oAuthTokenHandler = new LeanOAuthTokenHandler(leanApiClient, new("Tastytrade", accountNumber, refreshToken: refreshToken), TimeSpan.FromMinutes(15));
        var leanTokenHandler = new TastytradeApiClient(baseUrl, oAuthTokenHandler, accountNumber);

        var tokenCredentials = leanTokenHandler.TokenProvider.GetAccessToken(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.IsNotNull(tokenCredentials.AccessToken, "Access token should not be null.");
            Assert.IsNotEmpty(tokenCredentials.AccessToken, "Access token should not be empty.");
            Assert.AreEqual(TokenType.Bearer, tokenCredentials.TokenType);
        });
    }

    [TestCase("AAPL", Resolution.Daily, "AAPL{=d}")]
    [TestCase(".SPX250919C5050", Resolution.Hour, ".SPX250919C5050{=h}")]
    [TestCase("BRK/B", Resolution.Tick, "BRK/B{=t}")]
    public void GetSymbolWithPeriodPostfix(string brokerageSymbol, Resolution resolution, string expectedSymbolPeriodType)
    {
        var actualSymbolPeriodType = resolution.GetSymbolWithPeriodPostfix(brokerageSymbol);
        Assert.AreEqual(expectedSymbolPeriodType, actualSymbolPeriodType);
    }

    /// <summary>
    /// Reproduces the reported futures liquidation bug (short ES, flatten by Buy) as a pure unit test:
    /// builds the outgoing leg the plugin would send to Tastytrade for a Buy market order issued while
    /// holding a short futures position, and asserts the leg action is <see cref="OrderAction.Buy"/>.
    /// Before the fix the plugin produced <see cref="OrderAction.Sell"/> — doubling the short.
    /// </summary>
    [Test]
    public void CreateLegsForFlatteningShortFutureUsesBuyAction()
    {
        var sp500EMini = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2026, 06, 19));

        var securityProvider = new SecurityProvider([], BrokerageName.Tastytrade, new());
        securityProvider.GetSecurity(sp500EMini).Holdings.SetHoldings(averagePrice: 6606.75m, quantity: -2m);

        using var brokerage = new TestTastytradeBrokerage(securityProvider);

        var buyToFlatten = new MarketOrder(sp500EMini, 2m, DateTime.UtcNow, tag: "ES EOD flatten");
        var legs = brokerage.CreateLegs(new List<Order> { buyToFlatten });

        Assert.AreEqual(1, legs.Count);
        Assert.AreEqual(OrderAction.Buy, legs[0].Action);
        Assert.AreEqual(2m, legs[0].Quantity);
    }

    /// <summary>
    /// Future options must use the Tastytrade 4-action form (BuyToOpen/SellToOpen/BuyToClose/SellToClose),
    /// not the short Buy/Sell outright form: per the Tastytrade docs, the short form
    /// "only applies to single leg outright futures trades." Closing a short FutureOption
    /// position with a Buy market order must emit <see cref="OrderAction.BuyToClose"/>.
    /// </summary>
    [Test]
    public void CreateLegsForFlatteningShortFutureOptionUsesBuyToCloseAction()
    {
        var sp500EMini = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2026, 06, 19));
        var sp500EMiniCall = Symbol.CreateOption(sp500EMini, sp500EMini.ID.Market, SecurityType.FutureOption.DefaultOptionStyle(), OptionRight.Call, 6200m, new DateTime(2026, 06, 19));

        var securityProvider = new SecurityProvider([], BrokerageName.Tastytrade, new());
        securityProvider.GetSecurity(sp500EMiniCall).Holdings.SetHoldings(averagePrice: 50m, quantity: -1m);

        using var brokerage = new TestTastytradeBrokerage(securityProvider);

        var buyToClose = new MarketOrder(sp500EMiniCall, 1m, DateTime.UtcNow, tag: "FOP flatten");
        var legs = brokerage.CreateLegs(new List<Order> { buyToClose });

        Assert.AreEqual(1, legs.Count);
        Assert.AreEqual(OrderAction.BuyToClose, legs[0].Action);
        Assert.AreEqual(1m, legs[0].Quantity);
    }

    private static IEnumerable<TestCaseData> LegTestData
    {
        get
        {
            #region Filled Buy
            var filledBuy = @"{
        ""action"": ""Buy to Open"",
        ""instrument-type"": ""Equity Option"",
        ""quantity"": 5,
        ""remaining-quantity"": 0,
        ""symbol"": ""NVDA  251017C00190000"",
        ""fills"": [
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.80-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 1
            },
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.81-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 2
            },
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.82-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 2
            }
        ]
    }".DeserializeKebabCase<Leg>();


            yield return new TestCaseData(new ActualLeg[1] { new ActualLeg(true, 0, filledBuy) },
                new ExpectedResult[1][]
                {
                    [
                        new (1, OrderStatus.PartiallyFilled),
                        new (2, OrderStatus.PartiallyFilled),
                        new (2, OrderStatus.Filled)
                    ]
                }).SetName("Buy: PartiallyFilled(1) => PartiallyFilled(2) => Filled(2)");

            #endregion

            #region Filled Sell
            var filledSell = @"{
        ""action"": ""Sell to Open"",
        ""instrument-type"": ""Equity Option"",
        ""quantity"": 5,
        ""remaining-quantity"": 0,
        ""symbol"": ""NVDA  251017C00190000"",
        ""fills"": [
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.80-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 5
            }
        ]
    }".DeserializeKebabCase<Leg>();

            yield return new TestCaseData(new ActualLeg[1] { new ActualLeg(true, 0, filledSell) },
                new ExpectedResult[1][]
                {
                    [
                        new (-5, OrderStatus.Filled)
                    ]
                }).SetName("Sell: Filled(-5)");

            #endregion

            #region PartialFilled

            var partialFilled = @"{
        ""action"": ""Buy to Open"",
        ""instrument-type"": ""Equity Option"",
        ""quantity"": 5,
        ""remaining-quantity"": 2,
        ""symbol"": ""NVDA  251017C00190000"",
        ""fills"": [
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.80-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 1
            },
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.81-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 2
            }
        ]
    }".DeserializeKebabCase<Leg>();

            yield return new TestCaseData(new ActualLeg[1] { new(true, 2, partialFilled) },
                new ExpectedResult[1][]
                {
                    [
                        new (1, OrderStatus.PartiallyFilled),
                        new (2, OrderStatus.PartiallyFilled)
                    ]
                }).SetName("PartialFilled: PartialFilled(1) => PartialFilled(2)");

            #endregion

            #region Several legs: PartiallyFilled(1) => PartiallyFilled(2) => Filled(2)

            var leg1 = @"{
        ""action"": ""Buy to Open"",
        ""instrument-type"": ""Equity Option"",
        ""quantity"": 5,
        ""remaining-quantity"": 3,
        ""symbol"": ""NVDA  251017C00190000"",
        ""fills"": [
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.80-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 1
            },
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.81-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 2
            }
        ]
    }".DeserializeKebabCase<Leg>();

            var leg2 = @"{
        ""action"": ""Buy to Open"",
        ""instrument-type"": ""Equity Option"",
        ""quantity"": 5,
        ""remaining-quantity"": 0,
        ""symbol"": ""NVDA  251017C00190000"",
        ""fills"": [
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.80-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 1
            },
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.81-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 2
            },
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.82-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 2
            }
        ]
    }".DeserializeKebabCase<Leg>();

            var expectedResult2 = new ExpectedResult(3, OrderStatus.Filled);

            yield return new TestCaseData(new ActualLeg[2] { new(true, 2, leg1), new(true, 0, leg2) },
    new ExpectedResult[2][]
    {
                    [
                        new (1, OrderStatus.PartiallyFilled),
                        new (2, OrderStatus.PartiallyFilled)
                    ],
                    [
                        new (2, OrderStatus.Filled)
                    ]
    }).SetName("Several legs: PartiallyFilled(1) => PartiallyFilled(2) => Filled(2)");

            #endregion

            #region Several legs: PartiallyFilled(1) => PartiallyFilled(2) => Empty Response => PartiallyFilled(1) => Filled(1)

            var leg_1 = @"{
        ""action"": ""Buy to Open"",
        ""instrument-type"": ""Equity Option"",
        ""quantity"": 5,
        ""remaining-quantity"": 3,
        ""symbol"": ""NVDA  251017C00190000"",
        ""fills"": [
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.80-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 1
            },
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.81-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 2
            }
        ]
    }".DeserializeKebabCase<Leg>();

            var leg_2 = @"{
        ""action"": ""Buy to Open"",
        ""instrument-type"": ""Equity Option"",
        ""quantity"": 5,
        ""remaining-quantity"": 3,
        ""symbol"": ""NVDA  251017C00190000"",
        ""fills"": [
        ]
    }".DeserializeKebabCase<Leg>();

            var leg_3 = @"{
        ""action"": ""Buy to Open"",
        ""instrument-type"": ""Equity Option"",
        ""quantity"": 5,
        ""remaining-quantity"": 0,
        ""symbol"": ""NVDA  251017C00190000"",
        ""fills"": [
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.80-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 1
            },
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.81-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 2
            },
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.82-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 1
            },
            {
                ""destination-venue"": ""TEST_A"",
                ""ext-exec-id"": ""79"",
                ""ext-group-fill-id"": ""0"",
                ""fill-id"": ""2_TW::TEST_A1::20251006.83-TEST_FILL"",
                ""fill-price"": ""2.01"",
                ""filled-at"": ""2025-10-06T17:12:45.157+00:00"",
                ""quantity"": 1
            }
        ]
    }".DeserializeKebabCase<Leg>();

            var expectedResult_3 = new ExpectedResult(3, OrderStatus.Filled);

            yield return new TestCaseData(new ActualLeg[3] { new(true, 2, leg_1), new(false, 2, leg_2), new(true, 0, leg_3) },
new ExpectedResult[3][]
{
                    [
                        new (1, OrderStatus.PartiallyFilled),
                        new (2, OrderStatus.PartiallyFilled)
                    ],
                    [

                    ],
                    [
                        new (1, OrderStatus.PartiallyFilled),
                        new (1, OrderStatus.Filled)
                    ]
}).SetName("Several legs: PartiallyFilled(1) => PartiallyFilled(2) => Empty Response => PartiallyFilled(1) => Filled(1)");

            #endregion

        }
    }

    public record ExpectedResult(decimal FilledQuantity, OrderStatus OrderStatus);

    public record ActualLeg(bool IsInvokeEvent, int ExpectedCacheCount, Leg Legs);

    /// <summary>
    /// Test-only <see cref="TastytradeBrokerage"/> subclass that injects a user-supplied
    /// <see cref="ISecurityProvider"/> and a symbol mapper without requiring any network
    /// authentication, so unit tests can exercise order-leg construction directly.
    /// </summary>
    private sealed class TestTastytradeBrokerage : TastytradeBrokerage
    {
        public TestTastytradeBrokerage(ISecurityProvider securityProvider)
        {
            _securityProvider = securityProvider;
            _symbolMapper = new TastytradeBrokerageSymbolMapper(null);
        }
    }

    [Test, TestCaseSource(nameof(LegTestData))]
    public void HandleFilledEvent(ActualLeg[] legs, ExpectedResult[][] expectedResults)
    {
        var processedFillIds = new Dictionary<int, HashSet<string>>();

        var nvda = Symbol.Create("NVDA", SecurityType.Equity, Market.USA);
        var nvdaOptionContract = Symbol.CreateOption(nvda, nvda.ID.Market, SecurityType.Option.DefaultOptionStyle(), OptionRight.Call, 190m, new(2025, 10, 17));

        var groupOrderManager = new GroupOrderManager(2, 1, 2m);
        var leanOrder = new ComboLimitOrder(nvdaOptionContract, 5, groupOrderManager.LimitPrice, new(2025, 10, 6), groupOrderManager);

        for (int i = 0; i < legs.Length; i++)
        {
            var leg = legs[i];
            Assert.AreEqual(leg.IsInvokeEvent, TastytradeBrokerage.TryGetFilledEvent(leg.Legs, leanOrder, processedFillIds, out var orderEvents));

            if (leg.IsInvokeEvent)
            {
                for (int j = 0; j < orderEvents.Count; j++)
                {
                    var expectedResult = expectedResults[i][j];
                    var orderEvent = orderEvents[j];
                    Assert.AreEqual(expectedResult.FilledQuantity, orderEvent.FillQuantity);
                    Assert.AreEqual(expectedResult.OrderStatus, orderEvent.Status);
                }

                if (leg.ExpectedCacheCount == 0)
                {
                    Assert.IsEmpty(processedFillIds);
                }
                else
                {
                    Assert.AreEqual(leg.ExpectedCacheCount, processedFillIds[leanOrder.Id].Count);
                }
            }
        }
    }
}