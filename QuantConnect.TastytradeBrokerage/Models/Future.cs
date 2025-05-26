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

using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Represents a basic view of a future instrument, including its symbol and streamer symbol used for real-time data.
/// </summary>
public readonly struct Future
{
    /// <summary>
    /// Gets the standard symbol for the future (e.g., "/ESM5").
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the streamer symbol for the future, used for streaming data (e.g., "/ESM25:XCME").
    /// </summary>
    public string StreamerSymbol { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Future"/> struct.
    /// </summary>
    /// <param name="symbol">The standard symbol for the future.</param>
    /// <param name="streamerSymbol">The streamer symbol used for real-time updates.</param>
    [JsonConstructor]
    public Future(string symbol, string streamerSymbol)
    {
        Symbol = symbol;
        StreamerSymbol = streamerSymbol;
    }
}
