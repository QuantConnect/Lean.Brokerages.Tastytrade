﻿/*
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
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.HistoricalData;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture, Ignore("Not implemented")]
public class TastytradeBrokerageHistoryProviderTests
{
    private static TestCaseData[] TestParameters
    {
        get
        {
            return new[]
            {
                    // valid parameters, example:
                    new TestCaseData(Symbols.BTCUSD, Resolution.Tick, TimeSpan.FromMinutes(1), TickType.Quote, typeof(Tick), false),
                    new TestCaseData(Symbols.BTCUSD, Resolution.Minute, TimeSpan.FromMinutes(10), TickType.Quote, typeof(QuoteBar), false),
                    new TestCaseData(Symbols.BTCUSD, Resolution.Daily, TimeSpan.FromDays(10), TickType.Quote, typeof(QuoteBar), false),

                    new TestCaseData(Symbols.BTCUSD, Resolution.Tick, TimeSpan.FromMinutes(1), TickType.Trade, typeof(Tick), false),
                    new TestCaseData(Symbols.BTCUSD, Resolution.Minute, TimeSpan.FromMinutes(10), TickType.Trade, typeof(TradeBar), false),
                    new TestCaseData(Symbols.BTCUSD, Resolution.Daily, TimeSpan.FromDays(10), TickType.Trade, typeof(TradeBar), false),

                    // invalid parameter, validate SecurityType more accurate
                    new TestCaseData(Symbols.SPY, Resolution.Hour, TimeSpan.FromHours(14), TickType.Quote, typeof(QuoteBar), true),

                    /// New Listed Symbol on Brokerage <see cref="Slice.SymbolChangedEvents"/>
                    new TestCaseData(Symbol.Create("SUSHIGBP", SecurityType.Crypto, Market.Coinbase), Resolution.Minute, TimeSpan.FromHours(2), TickType.Trade, typeof(TradeBar), false),

                    /// Symbol was delisted form Brokerage (can return history data or not) <see cref="Slice.Delistings"/>
                    new TestCaseData(Symbol.Create("SNTUSD", SecurityType.Crypto, Market.Coinbase), Resolution.Daily, TimeSpan.FromDays(14), TickType.Trade, typeof(TradeBar), true),
                };
        }
    }

    [Test, TestCaseSource(nameof(TestParameters))]
    public void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, TickType tickType, Type dataType, bool throwsException)
    {
        TestDelegate test = () =>
        {
            var brokerage = new TastytradeBrokerage();

            var historyProvider = new BrokerageHistoryProvider();
            historyProvider.SetBrokerage(brokerage);
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null,
                null, null, null, null,
                false, null, null, null));

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

            var historyArray = historyProvider.GetHistory(requests, TimeZones.Utc).ToArray();
            foreach (var slice in historyArray)
            {
                if (resolution == Resolution.Tick)
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

            if (historyProvider.DataPointCount > 0)
            {
                // Ordered by time
                Assert.That(historyArray, Is.Ordered.By("Time"));

                // No repeating bars
                var timesArray = historyArray.Select(x => x.Time).ToArray();
                Assert.AreEqual(timesArray.Length, timesArray.Distinct().Count());
            }

            Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);
        };

        if (throwsException)
        {
            Assert.Throws<ArgumentException>(test);
        }
        else
        {
            Assert.DoesNotThrow(test);
        }
    }
}