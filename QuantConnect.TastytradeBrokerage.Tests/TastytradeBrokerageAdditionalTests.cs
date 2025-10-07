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
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Api;
using QuantConnect.Brokerages.Authentication;
using Leg = QuantConnect.Brokerages.Tastytrade.Models.Orders.Leg;

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

        var leanTokenHandler = new TastytradeApiClient(baseUrl, "Tastytrade", accountNumber, refreshToken, leanApiClient);

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