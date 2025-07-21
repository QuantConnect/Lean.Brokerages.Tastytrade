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
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture]
public class TastytradeBrokerageSymbolMapperTests
{
    /// <summary>
    /// Provides the mapping between Lean symbols and brokerage specific symbols.
    /// </summary>
    private TastytradeBrokerageSymbolMapper _symbolMapper;

    private TastyTradeBrokerageSymbolMapperStub _symbolMapperStub;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _symbolMapper = new(TestSetup.CreateTastytradeApiClient());
        _symbolMapperStub = new(_symbolMapper);
    }

    private static IEnumerable<TestCaseData> BrokerageSymbolTestCases
    {
        get
        {
            var aapl = Symbols.AAPL;
            yield return new("AAPL", InstrumentType.Equity, "AAPL", aapl);
            var aaplOptionContract = Symbol.CreateOption(aapl, aapl.ID.Market, SecurityType.Option.DefaultOptionStyle(), OptionRight.Call, 200m, new DateTime(2025, 06, 20));
            yield return new("AAPL  250620C00200000", InstrumentType.EquityOption, "AAPL", aaplOptionContract);

            var BRK_B = Symbol.Create("BRK.B", SecurityType.Equity, Market.USA);
            yield return new("BRK/B", InstrumentType.Equity, "BRK/B", BRK_B);
            var BRK_B_OptionContract = Symbol.CreateOption(BRK_B, BRK_B.ID.Market, SecurityType.Option.DefaultOptionStyle(), OptionRight.Call, 190m, new DateTime(2025, 06, 20));
            yield return new("BRKB  250620C00190000", InstrumentType.EquityOption, "BRK/B", BRK_B_OptionContract);

            var spx = Symbols.SPX;
            var spxOptionContract = Symbol.CreateOption(spx, spx.ID.Market, SecurityType.IndexOption.DefaultOptionStyle(), OptionRight.Call, 5635m, new DateTime(2025, 06, 20));
            yield return new("SPX   250620C05635000", InstrumentType.EquityOption, "SPX", spxOptionContract);

            var spxw = Symbol.CreateOption(spx, "SPXW", spx.ID.Market, SecurityType.IndexOption.DefaultOptionStyle(), OptionRight.Call, 1400m, new DateTime(2025, 05, 30));
            yield return new("SPXW  250530C01400000", InstrumentType.EquityOption, "SPX", spxw);

            var SP500EMini = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2025, 06, 20));
            yield return new("/ESM5", InstrumentType.Future, "/ES", SP500EMini);

            var SP500EMini2 = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2025, 09, 19));
            var SP500EMini_OptionContract = Symbol.CreateOption(SP500EMini2, SP500EMini.ID.Market, SecurityType.FutureOption.DefaultOptionStyle(), OptionRight.Put, 900m, new DateTime(2025, 09, 19));
            yield return new("./ESU5 ESU5  250919P900", InstrumentType.FutureOption, "ES", SP500EMini_OptionContract);

            var SP500EMini3 = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2026, 03, 20));
            var SP500EMini_OptionContract3 = Symbol.CreateOption(SP500EMini3, SP500EMini.ID.Market, SecurityType.FutureOption.DefaultOptionStyle(), OptionRight.Call, 4100m, new DateTime(2026, 03, 20));
            yield return new("./ESH6 ESH6  260320C4100", InstrumentType.FutureOption, "ES", SP500EMini_OptionContract3);

            yield return new("BTC/USD", InstrumentType.Cryptocurrency, "", default);

            var treasuryBondFutures = Symbol.CreateFuture(Futures.Financials.Y30TreasuryBond, Market.CBOT, new DateTime(2025, 9, 19));
            var treasuryBondFutures_OptionContract = Symbol.CreateOption(treasuryBondFutures, treasuryBondFutures.ID.Market, SecurityType.FutureOption.DefaultOptionStyle(), OptionRight.Call, 142.5m, new DateTime(2025, 06, 20));
            yield return new("./ZBU5 OZBN5 250620C142.5", InstrumentType.FutureOption, default, treasuryBondFutures_OptionContract);
        }
    }

    [Test, TestCaseSource(nameof(BrokerageSymbolTestCases))]
    public void ReturnsCorrectLeanSymbol(string brokerageSymbol, InstrumentType instrumentType, string optionUnderlyingSymbol, Symbol expectedLeanSymbol)
    {
        if (!_symbolMapperStub.TryGetLeanSymbol(brokerageSymbol, instrumentType, out var actualLeanSymbol, optionUnderlyingSymbol) && expectedLeanSymbol != default)
        {
            Assert.Fail($"Symbol mapping failed for brokerageSymbol='{brokerageSymbol}', instrumentType='{instrumentType}', " +
                $"optionUnderlyingSymbol='{optionUnderlyingSymbol}', expected Lean symbol='{expectedLeanSymbol}'.");
            return;
        }

        Assert.AreEqual(expectedLeanSymbol, actualLeanSymbol);
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
            var SP500EMini2 = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2025, 09, 19));
            yield return new(Symbol.CreateOption(SP500EMini2, SP500EMini2.ID.Market, SecurityType.FutureOption.DefaultOptionStyle(), OptionRight.Put, 900m, new DateTime(2025, 09, 19)), "./ESU5 ESU5  250919P900", "./ESU25P900:XCME");

            // GC - Future
            var Gold = Symbol.CreateFuture(Futures.Metals.Gold, Market.COMEX, new DateTime(2025, 12, 29));
            yield return new(Gold, "/GCZ5", "/GCZ25:XCEC");
            yield return new(Symbol.CreateOption(Gold, Gold.ID.Market, SecurityType.FutureOption.DefaultOptionStyle(), OptionRight.Put, 2040m, new DateTime(2025, 10, 28)), "./GCZ5 OGX5  251028P2040", "./OGX25P2040:XCEC");

            // GBP - Future
            var GBP = Symbol.CreateFuture(Futures.Currencies.GBP, Market.CME, new DateTime(2025, 12, 15));
            yield return new(GBP, "/6BZ5", "/6BZ25:XCME");

            // Gasoline - Future
            var Gasoline = Symbol.CreateFuture(Futures.Energy.Gasoline, Market.NYMEX, new DateTime(2025, 06, 1));
            yield return new(Gasoline, "/RBN5", "/RBN25:XNYM");

            var treasuryBondFutures = Symbol.CreateFuture(Futures.Financials.Y30TreasuryBond, Market.CBOT, new DateTime(2025, 9, 19));
            yield return new(Symbol.CreateOption(treasuryBondFutures, treasuryBondFutures.ID.Market, SecurityType.FutureOption.DefaultOptionStyle(), OptionRight.Call, 142.5m, new DateTime(2025, 06, 20)), "./ZBU5 OZBN5 250620C142.5", "./OZBN25C142.5:XCBT");

            var euroDollar = Symbol.CreateFuture(Futures.Financials.EuroDollar, Market.CME, new DateTime(2030, 06, 17));
            yield return new TestCaseData(euroDollar, "/GEM0", "/GEM30:XCME");
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

    [Test]
    public void MapsGoldFutureOptionToBrokerageSymbolsAndBack()
    {
        var expectedBrokerageSymbol = "./GCQ5 OGN5  250625P2915";
        var expectedStreamBrokerageSymbol = "./OGN25P2915:XCEC";

        var goldExpiry = new DateTime(2025, 8, 27);
        var gold = Symbol.CreateFuture(Futures.Metals.Gold, Market.COMEX, goldExpiry);
        var expectedOptionContractExpiry = new DateTime(2025, 06, 25);
        var goldOptionContract = Symbol.CreateOption(gold, gold.ID.Market, SecurityType.FutureOption.DefaultOptionStyle(), OptionRight.Put, 2915m, expectedOptionContractExpiry);

        var actualLeanSymbol = _symbolMapper.GetLeanSymbol(expectedBrokerageSymbol, SecurityType.FutureOption);

        Assert.AreEqual(goldOptionContract, actualLeanSymbol);
        Assert.AreEqual(goldExpiry, actualLeanSymbol.Underlying.ID.Date);
        Assert.AreEqual(expectedOptionContractExpiry, actualLeanSymbol.ID.Date);

        var (actualBrokerageSymbol, actualStreamBrokerageSymbol) = _symbolMapper.GetBrokerageSymbols(actualLeanSymbol);

        Assert.AreEqual(expectedBrokerageSymbol, actualBrokerageSymbol);
        Assert.AreEqual(expectedStreamBrokerageSymbol, actualStreamBrokerageSymbol);
    }

    public static IEnumerable<TestCaseData> GetFutureSymbolsTestCases()
    {
        // Natural gas futures expire the month previous to the contract month:
        // Expiry: August -> Contract month: September (U)
        yield return new TestCaseData("/NGU5", Symbol.CreateFuture(Futures.Energy.NaturalGas, Market.NYMEX, new DateTime(2025, 08, 27)));
        // Expiry: December 2025 -> Contract month: January (U) 2026 (26)
        yield return new TestCaseData("/NGF6", Symbol.CreateFuture(Futures.Energy.NaturalGas, Market.NYMEX, new DateTime(2025, 12, 29)));

        // BrentLastDayFinancial futures expire two months previous to the contract month:
        // Expiry: August -> Contract month: October (V)
        yield return new TestCaseData("/BZV5", Symbol.CreateFuture(Futures.Energy.BrentLastDayFinancial, Market.NYMEX, new DateTime(2025, 08, 29)));
        // Expiry: November 2025 -> Contract month: January (F) 2026 (26)
        yield return new TestCaseData("/BZF6", Symbol.CreateFuture(Futures.Energy.BrentLastDayFinancial, Market.NYMEX, new DateTime(2025, 11, 28)));
        // Expiry: December 2025 -> Contract month: February (G) 2026 (26)
        yield return new TestCaseData("/BZG6", Symbol.CreateFuture(Futures.Energy.BrentLastDayFinancial, Market.NYMEX, new DateTime(2025, 12, 31)));

        yield return new TestCaseData("/GEM0", Symbol.CreateFuture(Futures.Financials.EuroDollar, Market.CME, new DateTime(2030, 06, 17)));
    }

    [TestCaseSource(nameof(GetFutureSymbolsTestCases))]
    public void ConvertsFutureSymbolRoundTrip(string brokerageSymbol, Symbol leanSymbol)
    {
        Assert.IsTrue(_symbolMapperStub.TryGetLeanSymbol(brokerageSymbol, InstrumentType.Future, out var convertedLeanSymbol));
        Assert.AreEqual(leanSymbol, convertedLeanSymbol);

        var convertedBrokerageSymbol = _symbolMapper.GetBrokerageSymbols(leanSymbol);
        Assert.AreEqual(brokerageSymbol, convertedBrokerageSymbol.brokerageSymbol);
    }

    /// <summary>
    /// Stub implementation of <see cref="TastytradeBrokerage"/> used for unit testing.
    /// Allows injecting a specific <see cref="TastytradeBrokerageSymbolMapper"/> instance
    /// to initialize only the symbol mapping functionality for isolated testing.
    /// </summary>
    private class TastyTradeBrokerageSymbolMapperStub : TastytradeBrokerage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TastyTradeBrokerageSymbolMapperStub"/> class
        /// with the provided symbol mapper. This constructor is intended for use in unit tests
        /// where only the symbol mapping functionality is needed.
        /// </summary>
        /// <param name="symbolMapper">The symbol mapper instance to inject for testing.</param>
        public TastyTradeBrokerageSymbolMapperStub(TastytradeBrokerageSymbolMapper symbolMapper)
        {
            _symbolMapper = symbolMapper ?? throw new ArgumentNullException(nameof(symbolMapper));
            _algorithm = new AlgorithmStub();
            _algorithm.Settings.IgnoreUnknownAssetHoldings = true;
        }
    }
}