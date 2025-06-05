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
using System.Text;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Tastytrade;

/// <summary>
/// Represents the Tastytrade <see cref="IDataQueueUniverseProvider"/> implementation.
/// </summary>
public partial class TastytradeBrokerage : IDataQueueUniverseProvider
{
    /// <summary>
    /// Method returns a collection of Symbols that are available at the data source.
    /// </summary>
    /// <param name="symbol">Symbol to lookup</param>
    /// <param name="includeExpired">Include expired contracts</param>
    /// <param name="securityCurrency">Expected security currency(if any)</param>
    /// <returns>Enumerable of Symbols, that are associated with the provided Symbol</returns>
    public IEnumerable<Symbol> LookupSymbols(Symbol symbol, bool includeExpired, string securityCurrency = null)
    {
        if (!symbol.SecurityType.IsOption())
        {
            Log.Error($"{nameof(TastytradeBrokerage)}.{nameof(LookupSymbols)}: The provided symbol is not an option. SecurityType: " + symbol.SecurityType);
            return [];
        }

        switch (symbol.SecurityType)
        {
            case SecurityType.Option:
                return GetOptionChains(symbol.Canonical.Value.Replace("?", string.Empty), SecurityType.Option, false);
            case SecurityType.IndexOption:
                return GetOptionChains(symbol.Canonical.Value.Replace("?", string.Empty), SecurityType.IndexOption, true);
            case SecurityType.FutureOption:
                return GetFutureOptionChains(symbol.ID.Symbol);
            default:
                throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Retrieves and maps option contracts for a given underlying ticker to Lean <see cref="Symbol"/> instances.
    /// </summary>
    /// <param name="underlyingTicker">The ticker symbol of the underlying asset (e.g., AAPL, SPX).</param>
    /// <param name="securityType">The security type of the options (e.g., <see cref="SecurityType.Option"/> or <see cref="SecurityType.IndexOption"/>).</param>
    /// <param name="isOptionIndex">Indicates whether the underlying asset is an index (true) or an equity (false).</param>
    /// <returns>An enumerable of Lean <see cref="Symbol"/> instances representing the option contracts.</returns>
    private IEnumerable<Symbol> GetOptionChains(string underlyingTicker, SecurityType securityType, bool isOptionIndex)
    {
        var optionChains = _tastytradeApiClient.GetOptionChains(underlyingTicker);
        foreach (var optionChain in optionChains)
        {
            yield return _symbolMapper.GetLeanSymbol(optionChain.Symbol, securityType, underlyingTicker, optionChain.StreamerSymbol, isOptionIndex);
        }
    }

    /// <summary>
    /// Retrieves the mapped Lean symbols for future option contracts associated with a given underlying symbol.
    /// Skips contracts that are missing a streamer symbol, as they cannot be used for real-time updates.
    /// </summary>
    /// <param name="underlyingSymbol">The underlying future symbol (e.g., "ES").</param>
    /// <returns>An enumerable of Lean <see cref="Symbol"/> instances for valid future option contracts.</returns>
    private IEnumerable<Symbol> GetFutureOptionChains(string underlyingSymbol)
    {
        var futureOptionChains = _tastytradeApiClient.GetFutureOptionChains(underlyingSymbol);
        var skippedSymbols = new StringBuilder();
        foreach (var optionContract in futureOptionChains)
        {
            if (string.IsNullOrEmpty(optionContract.StreamerSymbol))
            {
                skippedSymbols.Append($"{optionContract.Symbol}, ");
                continue;
            }
            yield return _symbolMapper.GetLeanSymbol(optionContract.Symbol, SecurityType.FutureOption, underlyingSymbol, optionContract.StreamerSymbol);
        }

        if (skippedSymbols.Length > 0)
        {
            Log.Debug($"{nameof(TastytradeBrokerage)}.{nameof(GetFutureOptionChains)}: Skipped the following option contracts for '{underlyingSymbol}' due to missing StreamerSymbol (real-time data not available):\n{skippedSymbols}");
        }
    }

    /// <summary>
    /// Returns whether selection can take place or not.
    /// </summary>
    /// <remarks>This is useful to avoid a selection taking place during invalid times, for example IB reset times or when not connected,
    /// because if allowed selection would fail since IB isn't running and would kill the algorithm</remarks>
    /// <returns>True if selection can take place</returns>
    public bool CanPerformSelection()
    {
        return IsConnected;
    }
}
