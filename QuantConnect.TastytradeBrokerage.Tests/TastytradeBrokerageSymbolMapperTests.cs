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
using QuantConnect.Tests;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture]
public class TastytradeBrokerageSymbolMapperTests
{
    /// <summary>
    /// Provides the mapping between Lean symbols and brokerage specific symbols.
    /// </summary>
    private TastytradeBrokerageSymbolMapper _symbolMapper;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _symbolMapper = new(TestSetup.CreateTastytradeApiClient());
    }

    [Test]
    public void ReturnsCorrectLeanSymbol()
    {

    }

    private static IEnumerable<TestCaseData> LeanSymbolTestCases
    {
        get
        {
            TestGlobals.Initialize();
            // TSLA - Equity
            var underlying = Symbol.Create("TSLA", SecurityType.Equity, Market.USA);
            yield return new TestCaseData(underlying, "TSLA", "TSLA");
            yield return new TestCaseData(Symbol.CreateOption(underlying, Market.USA, OptionStyle.American, OptionRight.Call, 345.0m, new DateTime(2025, 06, 20)), "TSLA  250620C00345000", ".TSLA250620C345");
            yield return new TestCaseData(Symbol.CreateOption(underlying, Market.USA, OptionStyle.American, OptionRight.Put, 5m, new DateTime(2026, 12, 18)), "TSLA  261218P00005000", ".TSLA261218P5");
            // SPX - Index
            var SPX = Symbol.Create("SPX", SecurityType.Index, Market.USA);
            yield return new TestCaseData(SPX, "SPX", "SPX");
            yield return new TestCaseData(Symbol.CreateOption(SPX, Market.USA, SecurityType.IndexOption.DefaultOptionStyle(), OptionRight.Call,
                5050m, new DateTime(2025, 09, 19)), "SPX   250919C05050000", ".SPX250919C5050");
            //// SPXW - Index
            yield return new TestCaseData(Symbol.CreateOption(SPX, "SPXW", Market.USA, SecurityType.IndexOption.DefaultOptionStyle(), OptionRight.Call,
                3500m, new DateTime(2025, 07, 18)), "SPXW  250718C03500000", ".SPXW250718C3500");
            // F - Equity
            var F = Symbol.Create("F", SecurityType.Equity, Market.USA);
            yield return new TestCaseData(F, "F", "F");
            yield return new TestCaseData(Symbol.CreateOption(F, Market.USA, SecurityType.Option.DefaultOptionStyle(), OptionRight.Call, 2.02m, new DateTime(2025, 06, 20)), "F     250620C00002020", ".F250620C2.02");
            yield return new TestCaseData(Symbol.CreateOption(F, Market.USA, SecurityType.Option.DefaultOptionStyle(), OptionRight.Put, 2.17m, new DateTime(2025, 06, 20)), "F     250620P00002170", ".F250620P2.17");
            // DJT - Equity
            var DJT = Symbol.Create("DJT", SecurityType.Equity, Market.USA);
            yield return new TestCaseData(DJT, "DJT", "DJT");
            yield return new TestCaseData(Symbol.CreateOption(DJT, Market.USA, SecurityType.Option.DefaultOptionStyle(), OptionRight.Call, 13.0m, new DateTime(2025, 12, 19)), "DJT   251219C00013000", ".DJT251219C13");
            // IRBT - Equity
            var IRBT = Symbol.Create("IRBT", SecurityType.Equity, Market.USA);
            yield return new TestCaseData(IRBT, "IRBT", "IRBT");
            yield return new TestCaseData(Symbol.CreateOption(IRBT, Market.USA, SecurityType.Option.DefaultOptionStyle(), OptionRight.Call, 22.5m, new DateTime(2026, 1, 16)), "IRBT  260116C00022500", ".IRBT260116C22.5");
            // GOOGL - Equity
            var GOOGL = Symbol.Create("GOOGL", SecurityType.Equity, Market.USA);
            yield return new TestCaseData(GOOGL, "GOOGL", "GOOGL");
            yield return new TestCaseData(Symbol.CreateOption(GOOGL, Market.USA, SecurityType.Option.DefaultOptionStyle(), OptionRight.Put, 235m, new DateTime(2027, 06, 17)), "GOOGL 270617P00235000", ".GOOGL270617P235");
            // BRK.B - Equity
            var BRK_B = Symbol.Create("BRK.B", SecurityType.Equity, Market.USA);
            yield return new TestCaseData(BRK_B, "BRK/B", "BRK/B");
            yield return new TestCaseData(Symbol.CreateOption(BRK_B, BRK_B.ID.Market, SecurityType.Option.DefaultOptionStyle(), OptionRight.Call, 200m, new DateTime(2025, 06, 20)), "BRKB  250620C00200000", ".BRKB250620C200");
            yield return new TestCaseData(Symbol.CreateOption(BRK_B, BRK_B.ID.Market, SecurityType.Option.DefaultOptionStyle(), OptionRight.Put, 690m, new DateTime(2026, 03, 20)), "BRKB  260320P00690000", ".BRKB260320P690");
            // AAM.U - Equity
            var AAM_U = Symbol.Create("AAM.U", SecurityType.Equity, Market.USA);
            yield return new TestCaseData(AAM_U, "AAM/U", "AAM/U");

            // ES - Future
            var SP500EMini = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2025, 06, 20));
            yield return new(SP500EMini, "/ESM5", "/ESM25:XCME");

            // GC - Future
            var Gold = Symbol.CreateFuture(Futures.Metals.Gold, Market.CME, new DateTime(2025, 12, 29));
            yield return new(Gold, "/GCZ5", "/GCZ25:XCEC");

            // GBP - Future
            var GBP = Symbol.CreateFuture(Futures.Currencies.GBP, Market.CME, new DateTime(2025, 12, 15));
            yield return new(GBP, "/6BZ5", "/6BZ25:XCME");

            // Gasoline - Future
            var Gasoline = Symbol.CreateFuture(Futures.Energy.Gasoline, Market.CME, new DateTime(2025, 06, 1));
            yield return new(Gasoline, "/RBM5", "/RBM25:XNYM");
        }
    }

    [Test, TestCaseSource(nameof(LeanSymbolTestCases))]
    public void ReturnsCorrectBrokerageSymbol(Symbol symbol, string expectedBrokerageSymbol, string expectedBrokerageStreamSymbol)
    {
        var (brokerageSymbol, brokerageStreamMarketDataSymbol) = _symbolMapper.GetBrokerageSymbols(symbol);

        Assert.IsNotNull(brokerageSymbol);
        Assert.IsNotEmpty(brokerageSymbol);
        Assert.AreEqual(expectedBrokerageSymbol, brokerageSymbol);
        Assert.IsNotEmpty(brokerageStreamMarketDataSymbol);
        Assert.IsNotNull(brokerageStreamMarketDataSymbol);
        Assert.AreEqual(expectedBrokerageStreamSymbol, brokerageStreamMarketDataSymbol);
    }
}