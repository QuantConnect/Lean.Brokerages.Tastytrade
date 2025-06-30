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
using QuantConnect.Data;
using QuantConnect.Tests;
using QuantConnect.Logging;
using Microsoft.CodeAnalysis;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Data.Fundamental;
using System.ComponentModel.DataAnnotations;
using NodaTime;
using QuantConnect.Util;
using System.Threading.Tasks;
using System.Collections.Concurrent;

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
        _historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, null, null, new AlgorithmSettings()));
    }

    private static IEnumerable<TestCaseData> HistoryTestParameters
    {
        get
        {
            yield return new TestCaseData(Symbols.AAPL, Resolution.Daily, TimeSpan.FromDays(10), TickType.Trade, typeof(TradeBar), false);
            yield return new TestCaseData(Symbols.AAPL, Resolution.Minute, TimeSpan.FromMinutes(100), TickType.Trade, typeof(TradeBar), false);
            yield return new TestCaseData(Symbols.AAPL, Resolution.Second, TimeSpan.FromMinutes(10), TickType.Trade, typeof(TradeBar), false);

            var aaplOptionContract = Symbol.CreateOption(Symbols.AAPL, Symbols.AAPL.ID.Market, SecurityType.Option.DefaultOptionStyle(), OptionRight.Call, 200m, new DateTime(2025, 06, 27));
            yield return new TestCaseData(aaplOptionContract, Resolution.Daily, TimeSpan.FromDays(30), TickType.Trade, typeof(TradeBar), false);
            yield return new TestCaseData(aaplOptionContract, Resolution.Daily, TimeSpan.FromDays(30), TickType.OpenInterest, typeof(OpenInterest), false);

            yield return new TestCaseData(Symbols.AAPL, Resolution.Tick, TimeSpan.FromMinutes(1), TickType.Trade, typeof(Tick), false);
            yield return new TestCaseData(Symbols.AAPL, Resolution.Minute, TimeSpan.FromMinutes(10), TickType.Trade, typeof(TradeBar), false);

            // invalid parameter, validate SecurityType more accurate
            yield return new TestCaseData(Symbols.BTCUSD, Resolution.Hour, TimeSpan.FromHours(14), TickType.Quote, typeof(QuoteBar), true);

            // invalid parameter, validate TickType more accurate
            yield return new TestCaseData(Symbols.AAPL, Resolution.Daily, TimeSpan.FromDays(10), TickType.Quote, typeof(QuoteBar), true);
        }
    }

    [Test, TestCaseSource(nameof(HistoryTestParameters))]
    public void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, TickType tickType, Type dataType, bool throwsException)
    {
        TestDelegate test = () =>
        {
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var now = DateTime.UtcNow;
            var requests = new[]
            {
                    new HistoryRequest(now.Add(-period),
                        now,
                        dataType,
                        symbol,
                        resolution,
                        marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType),
                        marketHoursDatabase.GetDataTimeZone(symbol.ID.Market, symbol, symbol.SecurityType),
                        resolution,
                        false,
                        false,
                        DataNormalizationMode.Adjusted,
                        tickType)
            };

            var historyArray = _historyProvider.GetHistory(requests, TimeZones.Utc).ToArray();
            foreach (var slice in historyArray)
            {
                if (resolution == Resolution.Tick || tickType == TickType.OpenInterest)
                {
                    foreach (var tick in slice.Ticks[symbol])
                    {
                        Log.Debug($"{tick}");
                    }
                }
                else if (slice.QuoteBars.TryGetValue(symbol, out var quoteBar))
                {
                    Log.Debug($"{quoteBar}");
                }
                else if (slice.Bars.TryGetValue(symbol, out var tradeBar))
                {
                    Log.Debug($"{tradeBar}");
                }
            }

            if (_historyProvider.DataPointCount > 0)
            {
                // Ordered by time
                Assert.That(historyArray, Is.Ordered.By("Time"));

                // No repeating bars
                var timesArray = historyArray.Select(x => x.Time).ToArray();
                Assert.AreEqual(timesArray.Length, timesArray.Distinct().Count());
            }

            Log.Trace("Data points retrieved: " + _historyProvider.DataPointCount);
        };

        if (throwsException)
        {
            Assert.Throws<ArgumentNullException>(test);
        }
        else
        {
            Assert.DoesNotThrow(test);
        }
    }

    [Test]
    public void GetHistoryConcurrentRequestsReturnsExpectedSlices()
    {
        var startDateTimeNY = new DateTime(2025, 05, 25, 09, 30, 00);
        var endDateTimeNY = new DateTime(2025, 06, 27, 15, 30, 00);

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
            Assert.IsTrue(historyData.All(d => d != null), "All returned data points should be non-null.");
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
}