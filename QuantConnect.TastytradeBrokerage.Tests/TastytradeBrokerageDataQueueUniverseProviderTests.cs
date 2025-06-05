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
using NUnit.Framework;
using QuantConnect.Tests;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture]
public class TastytradeBrokerageDataQueueUniverseProviderTests
{
    private TastytradeBrokerage _brokerage;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _brokerage = TestSetup.CreateBrokerage(null, null);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _brokerage.Disconnect();
        _brokerage.Dispose();
    }

    private static IEnumerable<TestCaseData> LookUpSymbolsTestParameters
    {
        get
        {
            yield return new TestCaseData(Symbols.AAPL);
            yield return new TestCaseData(Symbol.Create("VIX", SecurityType.Index, Market.USA));
            yield return new TestCaseData(Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2026, 03, 20)));
        }
    }

    [Test, TestCaseSource(nameof(LookUpSymbolsTestParameters))]
    public void LookUpSymbols(Symbol symbol)
    {
        var option = Symbol.CreateCanonicalOption(symbol);

        var optionChain = _brokerage.LookupSymbols(option, false).ToList();

        Assert.IsNotNull(optionChain);
        Assert.True(optionChain.Any());
        Assert.Greater(optionChain.Count, 0);
        Assert.That(optionChain.Distinct().ToList().Count, Is.EqualTo(optionChain.Count));
    }
}
