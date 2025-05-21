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

using NUnit.Framework;
using QuantConnect.Util;
using System.Threading.Tasks;
using QuantConnect.Interfaces;

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
    public async Task GetAccountBalances()
    {
        var res = await _tastytradeApiClient.GetAccountBalances();

        Assert.IsNotNull(res);
        Assert.GreaterOrEqual(res.CashBalance, 0m);
        Assert.AreEqual(Currencies.USD, res.Currency);
        Assert.GreaterOrEqual(res.CashSettleBalance, 0m);
        Assert.GreaterOrEqual(res.AvailableTradingFunds, 0m);
    }

    [Test]
    public async Task GetAccountPositions()
    {
        var res = await _tastytradeApiClient.GetAccountPositions();

        Assert.IsNotNull(res);
    }

    [Test]
    public async Task GetApiQuoteToken()
    {
        var res = await _tastytradeApiClient.GetApiQuoteToken();

        Assert.IsNotNull(res);
        Assert.AreEqual("demo", res.Level);
    }
}