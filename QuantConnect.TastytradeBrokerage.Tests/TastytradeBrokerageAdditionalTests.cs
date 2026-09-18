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
using QuantConnect.Api;
using System.Threading;
using System.Threading.Tasks;
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
using QuantConnect.Brokerages.Tastytrade.Tests.Models;
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

    [Test, Explicit("Requires valid Tastytrade credentials and holds a live DxLink connection idle for 5 minutes.")]
    public void MarketDataWebSocketWhenIdleForFiveMinutesStaysConnected()
    {
        // Arrange
        using var connectionLost = new ManualResetEventSlim(false);
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
        }, _ => { }, (_, messageType, reason) => Log.Trace($"IdleSoak: {messageType}: {reason}"));
        webSocket.Open += (_, _) =>
        {
            var count = Interlocked.Increment(ref openCount);
            Log.Trace($"IdleSoak: open #{count}");
            if (count > 1)
            {
                connectionLost.Set();
            }
        };
        webSocket.Error += (_, e) =>
        {
            Log.Trace($"IdleSoak: error #{Interlocked.Increment(ref errorCount)}: {e.Message}");
            connectionLost.Set();
        };
        webSocket.Closed += (_, _) => Log.Trace("IdleSoak: closed");

        try
        {
            // Act
            webSocket.Connect();
            // Returns early on an error or a second open; otherwise the full 5 minutes pass with the socket idle.
            connectionLost.Wait(TimeSpan.FromMinutes(5));

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

    [Test]
    public void OrderPlacedByLeanAndReplacedByLeanRaisesNoBrokerageMessage()
    {
        var replaceOrderResponse =
            """{"data":{"id":507663525,"account-number":"5WY00000","cancellable":true,"contingent-status":"Pending Order","editable":true,"edited":false,"global-request-id":"f22472ba7241ba1bc74a4f8d7f1a4843","leg-count":1,"order-type":"Limit","price":"9.5","price-effect":"Debit","received-at":"2026-09-18T17:25:36.936+00:00","replaces-order-id":507663517,"size":1,"source":"QuantConnect","status":"Contingent","time-in-force":"GTC","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789752336936,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"context":"/accounts/5WY00000/orders/507663517"}""";
        // The account stream messages that arrived while the replace request was in flight, in the order the socket sent them:
        var oldIdLiveMessage =
            """{"type":"Order","data":{"id":507663517,"account-number":"5WY00000","cancellable":true,"editable":true,"edited":false,"ext-client-order-id":"JAAAC1KKYbvjKr8OMP","global-request-id":"d605e9b482b8e7f5900dba23a16cb573","leg-count":1,"order-type":"Limit","price":"10.0","price-effect":"Debit","received-at":"2026-09-18T17:25:36.128+00:00","size":1,"source":"QuantConnect","status":"Live","time-in-force":"GTC","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789752336218,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"timestamp":1789752336223,"ws-sequence":2}""";
        var oldIdCancelledMessage =
            """{"type":"Order","data":{"id":507663517,"account-number":"5WY00000","cancellable":false,"cancelled-at":"2026-09-18T17:25:36.965+00:00","cancelled-size":"1.0","editable":false,"edited":true,"ext-client-order-id":"JAAAC1KKYbvjKr8OMP","global-request-id":"d605e9b482b8e7f5900dba23a16cb573","leg-count":1,"order-type":"Limit","price":"10.0","price-effect":"Debit","received-at":"2026-09-18T17:25:36.128+00:00","replacing-order-id":507663525,"size":1,"source":"QuantConnect","status":"Cancelled","terminal-at":"2026-09-18T17:25:36.982+00:00","time-in-force":"GTC","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789752336995,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"timestamp":1789752337001,"ws-sequence":3}""";
        var newIdRoutedMessage =
            """{"type":"Order","data":{"id":507663525,"account-number":"5WY00000","cancellable":false,"editable":false,"edited":false,"global-request-id":"f22472ba7241ba1bc74a4f8d7f1a4843","leg-count":1,"order-type":"Limit","price":"9.5","price-effect":"Debit","received-at":"2026-09-18T17:25:36.936+00:00","replaces-order-id":507663517,"size":1,"source":"QuantConnect","status":"Routed","time-in-force":"GTC","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789752337093,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"timestamp":1789752337105,"ws-sequence":4}""";
        var newIdLiveMessage =
            """{"type":"Order","data":{"id":507663525,"account-number":"5WY00000","cancellable":true,"editable":true,"edited":false,"ext-client-order-id":"JAAAC1KKZHBP755q0g","global-request-id":"f22472ba7241ba1bc74a4f8d7f1a4843","leg-count":1,"order-type":"Limit","price":"9.5","price-effect":"Debit","received-at":"2026-09-18T17:25:36.936+00:00","replaces-order-id":507663517,"size":1,"source":"QuantConnect","status":"Live","time-in-force":"GTC","underlying-instrument-type":"Equity","underlying-symbol":"NOK","updated-at":1789752337143,"legs":[{"action":"Buy to Open","instrument-type":"Equity","quantity":1,"remaining-quantity":1,"symbol":"NOK","fills":[]}]},"timestamp":1789752337156,"ws-sequence":5}""";

        var httpHandler = new MockHttpMessageHandler();
        httpHandler.SetResponse(HttpMethod.Patch, "/orders/507663517", replaceOrderResponse);

        using var brokerage = new TestableTastytradeBrokerage(httpHandler: httpHandler);
        using var orderIdChanged = new ManualResetEventSlim(false);
        var notifications = 0;
        var messages = new List<BrokerageMessageEvent>();
        brokerage.NewBrokerageOrderNotification += (_, _) => notifications++;
        brokerage.Message += (_, message) => messages.Add(message);
        brokerage.OrderIdChanged += (_, _) => orderIdChanged.Set();

        // The order Lean placed: its Routed update already released PlaceOrder.
        var leanOrder = new LimitOrder(Symbol.Create("NOK", SecurityType.Equity, Market.USA), 1m, 10m, new DateTime(2026, 9, 18, 17, 25, 35, DateTimeKind.Utc));
        leanOrder.BrokerId.Add("507663517");
        brokerage.OrderProvider.Add(leanOrder);

        // UpdateOrder waits for the new id on the account stream, so it runs aside while the messages arrive.
        leanOrder.ApplyUpdateOrderRequest(new UpdateOrderRequest(new DateTime(2026, 9, 18, 17, 25, 35, DateTimeKind.Utc), leanOrder.Id, new UpdateOrderFields { Quantity = 2m, LimitPrice = 9.5m }));
        var updateOrder = Task.Run(() => brokerage.UpdateOrder(leanOrder));
        Assert.That(orderIdChanged.Wait(TimeSpan.FromSeconds(10)), Is.True, "Lean order: the replace request did not change the brokerage id.");

        brokerage.ReceiveAccountStreamMessage(oldIdLiveMessage);
        brokerage.ReceiveAccountStreamMessage(oldIdCancelledMessage);
        brokerage.ReceiveAccountStreamMessage(newIdRoutedMessage);
        brokerage.ReceiveAccountStreamMessage(newIdLiveMessage);
        Assert.That(updateOrder.Wait(TimeSpan.FromSeconds(10)) && updateOrder.Result, Is.True, "Lean order: UpdateOrder did not finish.");

        // Assert: the last updates of the old id are ignored
        Assert.That(messages, Is.Empty, "Replaced order: a brokerage message was raised.");
        Assert.That(notifications, Is.EqualTo(0), "Replaced order: notified as an order placed outside Lean.");
        Assert.That(brokerage.OrderProvider.OrdersCount, Is.EqualTo(1), "Replaced order: Lean has more than its own order.");

        // Assert: the order Lean updated
        Assert.That(leanOrder.BrokerId, Is.EqualTo(new[] { "507663525" }), "Lean order: wrong brokerage id.");
        var leanOrderEvents = brokerage.OrderProvider.GetOrderTicket(leanOrder.Id).OrderEvents;
        Assert.That(leanOrderEvents.Select(orderEvent => orderEvent.Status), Is.EqualTo(new[] { OrderStatus.UpdateSubmitted }), "Lean order: wrong order events.");
    }
}