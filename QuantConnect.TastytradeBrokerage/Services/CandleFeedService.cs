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
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Tastytrade.Services;

/// <summary>
/// Manages and stores candle data for a given symbol and resolution,
/// handling data ordering and signaling when the initial snapshot is complete.
/// </summary>
public class CandleFeedService : IDisposable
{
    /// <summary>
    /// Gets the symbol this service is tracking.
    /// </summary>
    private readonly Symbol _symbol;

    /// <summary>
    /// Gets the time zone associated with the symbol's exchange.
    /// Used for consistent time stamping.
    /// </summary>
    private readonly DateTimeZone _symbolDateTimeZone;

    /// <summary>
    /// Internal list storing candle data in descending order by time (newest first).
    /// </summary>
    private readonly List<BaseData> _dataDescendingOrder = [];

    /// <summary>
    /// The duration of each candle period, derived from the resolution.
    /// </summary>
    public readonly TimeSpan period;

    /// <summary>
    /// The history tick type.
    /// </summary>
    public readonly TickType tickType;

    /// <summary>
    /// Gets the candle data ordered ascending by time (oldest first).
    /// </summary>
    public IEnumerable<BaseData> Candles => _dataDescendingOrder.OrderBy(d => d.Time);

    /// <summary>
    /// Event that signals when the initial snapshot of candle data has been fully received.
    /// Since data arrives in descending order, this allows consumers to know when to reorder.
    /// </summary>
    public AutoResetEvent SnapshotCompletedEvent = new(false);

    /// <summary>
    /// Initializes a new instance of the <see cref="CandleFeedService"/> class
    /// for a specified symbol and resolution.
    /// </summary>
    /// <param name="symbol">The symbol to track candle data for.</param>
    /// <param name="resolution">The resolution defining the candle period.</param>
    /// <param name="tickType">The history requested Tick Type.</param>
    public CandleFeedService(Symbol symbol, Resolution resolution, TickType tickType)
    {
        _symbol = symbol;
        period = resolution.ToTimeSpan();
        this.tickType = tickType;
        _symbolDateTimeZone = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;
    }

    /// <summary>
    /// Adds a new candle data point to the collection.
    /// </summary>
    /// <param name="dateTime">The UTC timestamp of the candle's start time.</param>
    /// <param name="open">The opening price.</param>
    /// <param name="high">The highest price during the candle period.</param>
    /// <param name="low">The lowest price during the candle period.</param>
    /// <param name="close">The closing price.</param>
    /// <param name="volume">The trading volume during the candle period.</param>
    public void Add(DateTime dateTime, decimal open, decimal high, decimal low, decimal close, decimal volume, decimal openInterest)
    {
        switch (tickType)
        {
            case TickType.Trade:
                _dataDescendingOrder.Add(new TradeBar(dateTime.ConvertFromUtc(_symbolDateTimeZone), _symbol, open, high, low, close, volume, period));
                break;
            case TickType.OpenInterest:
                _dataDescendingOrder.Add(new OpenInterest(dateTime.ConvertFromUtc(_symbolDateTimeZone), _symbol, openInterest));
                break;
            default:
                throw new NotSupportedException($"{nameof(CandleFeedService)}.{nameof(Add)}: Unsupported TickType '{tickType}' for symbol '{_symbol}'. ");
        }
    }

    /// <summary>
    /// Releases all resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        SnapshotCompletedEvent?.Dispose();
    }
}
