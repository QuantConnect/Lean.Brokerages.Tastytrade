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
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Brokerages.Tastytrade.Services;

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Provides a synchronization and resource container for managing a symbol's candle feed service during historical data operations.
/// </summary>
public class CandleFeedContext : IDisposable
{
    /// <summary>
    /// Event used to signal the completion of the current historical data operation for the symbol.
    /// Other threads waiting on this symbol will be released when the event is set.
    /// </summary>
    public ManualResetEvent FinishResetEvent { get; } = new(false);

    /// <summary>
    /// The <see cref="CandleFeedService"/> instance associated with this symbol, responsible for managing candle data feeds.
    /// </summary>
    public CandleFeedService CandleFeedService { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CandleFeedContext"/> class for the specified history request.
    /// </summary>
    /// <param name="historyRequest">The history request containing symbol, resolution, and tick type information.</param>

    public CandleFeedContext(HistoryRequest histroyRequest)
    {
        CandleFeedService = new CandleFeedService(histroyRequest.Symbol, histroyRequest.Resolution, histroyRequest.TickType);
    }

    /// <summary>
    /// Disposes the synchronization event and candle feed service associated with this context.
    /// Safe to call multiple times.
    /// </summary>
    public void Dispose()
    {
        FinishResetEvent.Dispose();
        CandleFeedService.Dispose();
    }
}
