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
using NodaTime;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Tests;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Lean.Engine.HistoricalData;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture]
public class TastytradeBrokerageHistoryProviderTests
{
    private BrokerageHistoryProvider _historyProvider;

    [SetUp]
    public void SetUp()
    {
        _historyProvider = new BrokerageHistoryProvider();
        _historyProvider.SetBrokerage(TestSetup.CreateBrokerage(null, null));
        _historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, null, null, new AlgorithmSettings() { DailyPreciseEndTime = true }));
    }

    private static IEnumerable<TestCaseData> ValidHistoryParameters
    {
        get
        {
            var AAPL = CreateSymbol("AAPL", SecurityType.Equity);
            yield return new TestCaseData(AAPL, Resolution.Minute, TickType.Trade, new DateTime(2025, 05, 30), new DateTime(2025, 06, 30), false);
            var endDate = DateTime.UtcNow.Date.AddHours(23);
            var startDate = DateTime.UtcNow.Date;
            yield return new TestCaseData(AAPL, Resolution.Minute, TickType.Trade, startDate, endDate, false);
            yield return new TestCaseData(AAPL, Resolution.Tick, TickType.Trade, startDate, endDate, false).SetName("AAPL,Tick,Trade");
            yield return new TestCaseData(AAPL, Resolution.Second, TickType.Trade, startDate, endDate, false).SetName("AAPL,Second,Trade");
            yield return new TestCaseData(AAPL, Resolution.Hour, TickType.Trade, new DateTime(2025, 06, 18), new DateTime(2025, 07, 1), false);
            yield return new TestCaseData(AAPL, Resolution.Daily, TickType.Trade, new DateTime(2025, 05, 18), new DateTime(2025, 07, 01), false);

            var SPX = Symbol.Create("SPX", SecurityType.Index, Market.USA);
            yield return new TestCaseData(SPX, Resolution.Minute, TickType.Trade, new DateTime(2025, 06, 1), new DateTime(2025, 07, 01), false);
            yield return new TestCaseData(SPX, Resolution.Minute, TickType.Trade, startDate, endDate, false);
            yield return new TestCaseData(SPX, Resolution.Hour, TickType.Trade, new DateTime(2025, 06, 1), new DateTime(2025, 07, 01), false);
            yield return new TestCaseData(SPX, Resolution.Daily, TickType.Trade, new DateTime(2024, 05, 18), new DateTime(2024, 07, 01), false);

            var spxOptionContract = Symbol.CreateOption(SPX, SPX.ID.Market, SecurityType.IndexOption.DefaultOptionStyle(), OptionRight.Call, 6195, new DateTime(2025, 07, 18));
            yield return new TestCaseData(spxOptionContract, Resolution.Minute, TickType.Trade, startDate, endDate, false);
            yield return new TestCaseData(spxOptionContract, Resolution.Tick, TickType.Trade, startDate, endDate, false).SetName("SPX_OptionContract,Tick,Trade");
            yield return new TestCaseData(spxOptionContract, Resolution.Second, TickType.Trade, startDate, endDate, false).SetName("SPX_OptionContract,Second,Trade");
            yield return new TestCaseData(spxOptionContract, Resolution.Hour, TickType.Trade, new DateTime(2025, 06, 18), new DateTime(2025, 07, 1), false);
            yield return new TestCaseData(spxOptionContract, Resolution.Daily, TickType.Trade, new DateTime(2025, 05, 18), new DateTime(2025, 07, 01), false);

            var AAPLOption = Symbol.CreateOption(AAPL, Market.USA, AAPL.SecurityType.DefaultOptionStyle(), OptionRight.Call, 200m, new DateTime(2025, 07, 03));
            yield return new TestCaseData(AAPLOption, Resolution.Tick, TickType.Trade, startDate, endDate, false).SetName("AAPL_OptionContract,Tick,Trade");
            yield return new TestCaseData(AAPLOption, Resolution.Second, TickType.Trade, startDate, endDate, false).SetName("AAPL_OptionContract,Second,Trade");
            yield return new TestCaseData(AAPLOption, Resolution.Minute, TickType.Trade, new DateTime(2025, 06, 1), new DateTime(2025, 07, 01), false);
            yield return new TestCaseData(AAPLOption, Resolution.Hour, TickType.Trade, new DateTime(2025, 06, 1), new DateTime(2025, 07, 01), false);
            yield return new TestCaseData(AAPLOption, Resolution.Daily, TickType.Trade, new DateTime(2025, 06, 1), new DateTime(2025, 07, 01), false);

            var SP500EMini = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2025, 09, 19));
            yield return new TestCaseData(SP500EMini, Resolution.Minute, TickType.Trade, new DateTime(2025, 05, 30), new DateTime(2025, 06, 30), false);
            yield return new TestCaseData(SP500EMini, Resolution.Minute, TickType.Trade, startDate, endDate, false);
            yield return new TestCaseData(SP500EMini, Resolution.Tick, TickType.Trade, startDate, endDate, false).SetName("SP500EMini,Tick,Trade");
            yield return new TestCaseData(SP500EMini, Resolution.Second, TickType.Trade, startDate, endDate, false).SetName("SP500EMini,Second,Trade");
            yield return new TestCaseData(SP500EMini, Resolution.Hour, TickType.Trade, new DateTime(2025, 06, 18), new DateTime(2025, 07, 1), false);
            yield return new TestCaseData(SP500EMini, Resolution.Daily, TickType.Trade, new DateTime(2025, 05, 18), new DateTime(2025, 07, 01), false);

            var Y10TreasuryNote = Symbol.CreateFuture(Futures.Financials.Y10TreasuryNote, Market.CBOT, new DateTime(2025, 09, 26));
            var Y10TreasuryNote_OptionContract = Symbol.CreateOption(Y10TreasuryNote, Y10TreasuryNote.ID.Market, SecurityType.FutureOption.DefaultOptionStyle(), OptionRight.Call, 111.75m, new DateTime(2025, 07, 03));
            yield return new TestCaseData(Y10TreasuryNote_OptionContract, Resolution.Daily, TickType.Trade, new DateTime(2025, 05, 01), new DateTime(2025, 07, 01), false);

            var SP500EMini_OptionContract = Symbol.CreateOption(SP500EMini, SP500EMini.ID.Market, SecurityType.FutureOption.DefaultOptionStyle(), OptionRight.Put, 6200m, new DateTime(2025, 09, 19));
            yield return new TestCaseData(SP500EMini_OptionContract, Resolution.Minute, TickType.Trade, new DateTime(2025, 05, 30), new DateTime(2025, 06, 30), false);
            yield return new TestCaseData(SP500EMini_OptionContract, Resolution.Tick, TickType.Trade, startDate, endDate, false).SetName("SP500EMini_OptionContract,Tick,Trade");
            yield return new TestCaseData(SP500EMini_OptionContract, Resolution.Second, TickType.Trade, startDate, endDate, false).SetName("SP500EMini_OptionContract,Second,Trade");
            yield return new TestCaseData(SP500EMini_OptionContract, Resolution.Hour, TickType.Trade, new DateTime(2025, 06, 18), new DateTime(2025, 07, 1), false);
            yield return new TestCaseData(SP500EMini_OptionContract, Resolution.Daily, TickType.Trade, new DateTime(2025, 05, 18), new DateTime(2025, 07, 01), false);

            yield return new TestCaseData(Symbols.BTCUSD, Resolution.Daily, TickType.Trade, default(DateTime), default(DateTime), true).SetDescription("Not Support Crypto contract history");

            yield return new TestCaseData(AAPL, Resolution.Second, TickType.Quote, new DateTime(2024, 10, 1), new DateTime(2024, 11, 8), true).SetDescription("Not Support Quote TickType request.");
            yield return new TestCaseData(AAPL, Resolution.Minute, TickType.Trade, new DateTime(2024, 11, 8), new DateTime(2024, 10, 1), true).SetDescription("StartDate > EndDate");
        }
    }

    [TestCaseSource(nameof(ValidHistoryParameters))]
    public void GetsHistory(Symbol symbol, Resolution resolution, TickType tickType, DateTime startDateTime, DateTime endDateTime, bool isNullResult)
    {
        var historyRequest = CreateHistoryRequest(symbol, resolution, tickType, startDateTime, endDateTime);

        var history = _historyProvider.GetHistory(new[] { historyRequest }, TimeZones.NewYork)?.ToList();

        if (isNullResult)
        {
            Assert.IsNull(history);
        }
        else
        {
            Assert.IsNotNull(history);
            Assert.IsNotEmpty(history);

            AssertTradeBars(history.SelectMany(t => t.Bars.Values), symbol, resolution.ToTimeSpan());

            if (_historyProvider.DataPointCount > 0)
            {
                // Ordered by time
                Assert.That(history, Is.Ordered.By("Time"));

                // No repeating bars
                var timesArray = history.Select(x => x.Time).ToArray();
                Assert.AreEqual(timesArray.Length, timesArray.Distinct().Count());
            }
        }
    }

    [Test]
    public void GetHistoryConcurrentRequestsReturnsExpectedSlices()
    {
        var startDateTimeNY = new DateTime(2025, 05, 25, 09, 30, 00);
        var endDateTimeNY = new DateTime(2025, 06, 27, 20, 0, 00);

        var requests = new Resolution[] { Resolution.Daily, Resolution.Hour }
            .SelectMany(r => Enumerable.Repeat(r, 4)
                .Select(_ => CreateHistoryRequest(Symbols.AAPL, r, TickType.Trade, startDateTimeNY, endDateTimeNY)))
            .ToList();

        var results = new ConcurrentBag<Slice[]>();

        // Act
        Parallel.ForEach(requests, request =>
        {
            var history = _historyProvider.GetHistory([request], TimeZones.Utc).ToArray();
            results.Add(history);
        });

        Assert.AreEqual(8, results.Count, "Expected 8 history results (2 resolutions x 4 each).");


        foreach (var historyData in results)
        {
            Assert.IsNotNull(historyData, "History result should not be null.");
            Assert.That(historyData, Is.Ordered.By("Time"), "History data should be ordered by time.");

            var timesArray = historyData.Select(x => x.Time).ToArray();
            Assert.AreEqual(timesArray.Length, timesArray.Distinct().Count(), "History data should not contain duplicate bars.");

            // Validate start and end boundaries
            if (timesArray.Length > 0)
            {
                var firstTimeUtc = timesArray.First();
                var lastTimeUtc = timesArray.Last();

                Assert.That(firstTimeUtc, Is.GreaterThanOrEqualTo(startDateTimeNY), "First bar time should be >= start time.");
                Assert.That(lastTimeUtc, Is.LessThanOrEqualTo(endDateTimeNY), "Last bar time should be <= end time.");
            }
        }
    }

    private static void AssertTradeBars(IEnumerable<TradeBar> tradeBars, Symbol symbol, TimeSpan period)
    {
        foreach (var tradeBar in tradeBars)
        {
            Assert.That(tradeBar.Symbol, Is.EqualTo(symbol));
            //Assert.That(tradeBar.Period, Is.EqualTo(period));
            Assert.That(tradeBar.Open, Is.GreaterThan(0));
            Assert.That(tradeBar.High, Is.GreaterThan(0));
            Assert.That(tradeBar.Low, Is.GreaterThan(0));
            Assert.That(tradeBar.Close, Is.GreaterThan(0));
            Assert.That(tradeBar.Price, Is.GreaterThan(0));
            Assert.That(tradeBar.Volume, Is.GreaterThanOrEqualTo(0));
            Assert.That(tradeBar.Time, Is.GreaterThan(default(DateTime)));
            Assert.That(tradeBar.EndTime, Is.GreaterThan(default(DateTime)));
        }
    }

    private static HistoryRequest CreateHistoryRequest(Symbol symbol, Resolution resolution, TickType tickType, DateTime startDateTime, DateTime endDateTime,
    SecurityExchangeHours exchangeHours = null, DateTimeZone dataTimeZone = null, bool includeExtendedMarketHours = true)
    {
        if (exchangeHours == null)
        {
            exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
        }

        if (dataTimeZone == null)
        {
            dataTimeZone = TimeZones.NewYork;
        }

        var dataType = LeanData.GetDataType(resolution, tickType);
        return new HistoryRequest(
            startDateTime.ConvertToUtc(exchangeHours.TimeZone),
            endDateTime.ConvertToUtc(exchangeHours.TimeZone),
            dataType,
            symbol,
            resolution,
            exchangeHours,
            dataTimeZone,
            resolution,
            includeExtendedMarketHours,
            false,
            DataNormalizationMode.Raw,
            tickType
            );
    }

    public static Symbol CreateSymbol(string ticker, SecurityType securityType, OptionRight? optionRight = null, decimal? strikePrice = null, DateTime? expirationDate = null, string market = Market.USA)
    {
        switch (securityType)
        {
            case SecurityType.Equity:
                return Symbol.Create(ticker, securityType, market);
            case SecurityType.Option:
                var underlyingEquitySymbol = Symbol.Create(ticker, SecurityType.Equity, market);
                return Symbol.CreateOption(underlyingEquitySymbol, market, SecurityType.Option.DefaultOptionStyle(), optionRight.Value, strikePrice.Value, expirationDate.Value);
            default:
                throw new NotSupportedException($"The security type '{securityType}' is not supported.");
        }
    }
}