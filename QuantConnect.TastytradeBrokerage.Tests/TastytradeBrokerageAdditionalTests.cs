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
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using QuantConnect.Brokerages.Tastytrade.Api;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

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
        var accountNumber = Config.Get("tastytrade-account-number");
        var refreshToken = Config.Get("tastytrade-refresh-token");

        var leanApiClient = new ApiConnection(Globals.UserId, Globals.UserToken);

        if (!leanApiClient.Connected)
        {
            throw new ArgumentException("Invalid api user id or token, cannot authenticate subscription.");
        }

        var leanTokenHandler = new LeanTokenHandler(leanApiClient, "Tastytrade", accountNumber, refreshToken);

        var (tokenType, accessToken) = leanTokenHandler.GetAccessToken(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.IsNotNull(accessToken, "Access token should not be null.");
            Assert.IsNotEmpty(accessToken, "Access token should not be empty.");
            Assert.AreEqual(TokenType.Bearer, tokenType);
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
}