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
using System.Threading.Tasks;
using System.Collections.Concurrent;
using QuantConnect.Brokerages.Tastytrade.Api;
using QuantConnect.Brokerages.Tastytrade.Models;

namespace QuantConnect.Brokerages.Tastytrade;

/// <summary>
/// Maps Lean symbols to Tastytrade-compatible brokerage symbols used for orders and streaming market data.
/// </summary>
public class TastytradeBrokerageSymbolMapper
{
    /// <summary>
    /// The Tastytrade API client used to query instrument and market data.
    /// </summary>
    private readonly TastytradeApiClient _tastytradeApiClient;

    /// <summary>
    /// A cache mapping Lean symbols to their corresponding brokerage and stream symbols.
    /// </summary>
    private static readonly ConcurrentDictionary<Symbol, BrokerageSymbols> _brokerageSymbolsByLeanSymbol = [];

    /// <summary>
    /// A cache mapping brokerage symbols to their corresponding Lean symbols.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Symbol> _leanSymbolByBrokerageSymbol = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TastytradeBrokerageSymbolMapper"/> class.
    /// </summary>
    /// <param name="tastytradeApiClient">The Tastytrade API client used to query future instruments.</param>
    public TastytradeBrokerageSymbolMapper(TastytradeApiClient tastytradeApiClient)
    {
        _tastytradeApiClient = tastytradeApiClient;
    }

    /// <summary>
    /// Converts a brokerage symbol back into a Lean symbol.
    /// </summary>
    /// <param name="brokerageSymbol">The brokerage symbol to convert.</param>
    /// <param name="securityType">The security type (e.g., Equity, Option, Future).</param>
    /// <param name="market">The market (e.g., "USA").</param>
    /// <param name="expirationDate">The expiration date for derivatives, if applicable.</param>
    /// <param name="strike">The strike price for options, if applicable.</param>
    /// <param name="optionRight">The option right (Call or Put), if applicable.</param>
    /// <returns>A new <see cref="Symbol"/> instance representing the Lean symbol.</returns>
    /// <exception cref="NotImplementedException">Always thrown. Functionality not implemented yet.</exception>
    public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string underlyingBrokerageSymbol)
    {
        if (_leanSymbolByBrokerageSymbol.TryGetValue(brokerageSymbol, out var leanSymbol))
        {
            return leanSymbol;
        }

        switch (securityType)
        {
            case SecurityType.Equity:
                leanSymbol = Symbol.Create(ToOptionLeanTickerFormat(brokerageSymbol), securityType, Market.USA);
                break;
            case SecurityType.Future:
                leanSymbol = SymbolRepresentation.ParseFutureSymbol(ToFutureLeanTickerFormat(brokerageSymbol));
                break;
            case SecurityType.Option:
                var isIndex = _tastytradeApiClient.IsUnderlyingEquityAnIndexAsync(underlyingBrokerageSymbol).SynchronouslyAwaitTaskResult();

                if (isIndex)
                {
                    leanSymbol = SymbolRepresentation.ParseOptionTickerOSI(brokerageSymbol, SecurityType.IndexOption, SecurityType.IndexOption.DefaultOptionStyle(), Market.USA);
                }
                else
                {
                    if (!SymbolRepresentation.TryDecomposeOptionTickerOSI(brokerageSymbol, out _, out var osiExpiry, out var osiRight, out var osiStrike))
                    {
                        throw new ArgumentException($"{nameof(TastytradeBrokerageSymbolMapper)}.{nameof(GetLeanSymbol)}: Failed to decompose option ticker '{brokerageSymbol}'. Ensure the symbol follows the correct OSI format.");
                    }

                    var underlyingSymbol = Symbol.Create(ToOptionLeanTickerFormat(underlyingBrokerageSymbol), SecurityType.Equity, Market.USA);
                    leanSymbol = Symbol.CreateOption(underlyingSymbol, Market.USA, securityType.DefaultOptionStyle(), osiRight, osiStrike, osiExpiry);
                }
                break;
            default:
                throw new NotImplementedException($"{nameof(TastytradeBrokerageSymbolMapper)}.{nameof(GetLeanSymbol)}: " +
                    $"The security type '{securityType}' with brokerage symbol '{brokerageSymbol}' is not supported.");
        }

        SynchronizeCachedSymbolCollection(leanSymbol, brokerageSymbol, default);

        return leanSymbol;
    }

    /// <summary>
    /// Returns the brokerage symbols used for orders and streaming data based on the Lean <see cref="Symbol"/>.
    /// </summary>
    /// <param name="symbol">The Lean symbol to map.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description>The brokerage symbol used for placing orders.</description></item>
    /// <item><description>The brokerage symbol used for streaming market data.</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown if the <paramref name="symbol"/> security type is not supported.</exception>
    public (string brokerageSymbol, string brokerageStreamMarketDataSymbol) GetBrokerageSymbols(Symbol symbol)
    {
        if (_brokerageSymbolsByLeanSymbol.TryGetValue(symbol, out var brokerageSymbols))
        {
            return (brokerageSymbols.BrokerageSymbol, brokerageSymbols.BrokerageStreamMarketDataSymbol);
        }

        var brokerageSymbol = default(string);
        var brokerageStreamMarketDataSymbol = default(string);
        switch (symbol.SecurityType)
        {
            case SecurityType.Equity:
            case SecurityType.Index:
                brokerageSymbol = ToBrokerageTickerFormat(symbol.Value);
                brokerageStreamMarketDataSymbol = brokerageSymbol;
                break;
            case SecurityType.Option:
            case SecurityType.IndexOption:
                (brokerageSymbol, brokerageStreamMarketDataSymbol) = GenerateOptionBrokerageSymbols(symbol);
                break;
            case SecurityType.Future:
                (brokerageSymbol, brokerageStreamMarketDataSymbol) = GenerateFutureBrokerageSymbols(symbol).SynchronouslyAwaitTaskResult();
                break;
            default:
                throw new NotSupportedException($"{nameof(TastytradeBrokerageSymbolMapper)}.{nameof(GetBrokerageSymbols)}.");
        }

        SynchronizeCachedSymbolCollection(symbol, brokerageSymbol, brokerageStreamMarketDataSymbol);

        return (brokerageSymbol, brokerageStreamMarketDataSymbol);
    }

    /// <summary>
    /// Ensures the internal symbol caches are synchronized with the provided Lean symbol,
    /// brokerage symbol, and brokerage stream symbol. If the stream symbol is not provided,
    /// it delegates synchronization to <see cref="GetBrokerageSymbols(Symbol)"/>.
    /// </summary>
    /// <param name="leanSymbol">The Lean symbol to associate with brokerage symbols.</param>
    /// <param name="brokerageSymbol">The brokerage-specific symbol.</param>
    /// <param name="brokerageStreamMarketDataSymbol">The brokerage stream symbol used for market data.</param>
    private void SynchronizeCachedSymbolCollection(Symbol leanSymbol, string brokerageSymbol, string brokerageStreamMarketDataSymbol)
    {
        if (string.IsNullOrEmpty(brokerageStreamMarketDataSymbol))
        {
            GetBrokerageSymbols(leanSymbol);
            // Synchronization happens within GetBrokerageSymbols if stream symbol is missing
            return;
        }

        _brokerageSymbolsByLeanSymbol[leanSymbol] = new BrokerageSymbols(brokerageSymbol, brokerageStreamMarketDataSymbol);
        _leanSymbolByBrokerageSymbol[brokerageSymbol] = leanSymbol;
    }

    /// <summary>
    /// Generates the brokerage and streaming symbols for an option or index option.
    /// </summary>
    /// <param name="symbol">The Lean option symbol.</param>
    /// <returns>A tuple with the brokerage symbol and the streaming market data symbol.</returns>
    private static (string brokerageSymbol, string brokerageStreamMarketDataSymbol) GenerateOptionBrokerageSymbols(Symbol symbol)
    {
        var underlying = ToOptionBrokerageTickerFormat(symbol.Canonical.Value.Replace("?", string.Empty));
        var expiryDate = symbol.ID.Date.ToStringInvariant(DateFormat.SixCharacter);
        var optionRight = symbol.ID.OptionRight.ToString()[0];

        if (underlying.Length > 5)
        {
            underlying += " ";
        }

        return ($"{underlying,-6}{expiryDate}{optionRight}{symbol.ID.StrikePrice * 1000m:00000000}", $".{underlying}{expiryDate}{optionRight}{symbol.ID.StrikePrice.ToStringInvariant("G29")}");
    }

    /// <summary>
    /// Asynchronously generates the brokerage and streaming symbols for a future.
    /// </summary>
    /// <param name="symbol">The Lean future symbol.</param>
    /// <returns>A tuple with the brokerage symbol and the streaming market data symbol.</returns>
    private async Task<(string brokerageSymbol, string brokerageStreamMarketDataSymbol)> GenerateFutureBrokerageSymbols(Symbol symbol)
    {
        var futureTicker = $"{symbol.ID.Symbol}{SymbolRepresentation.FuturesMonthLookup[symbol.ID.Date.Month]}{symbol.ID.Date.ToString("yy").Last()}";

        var future = await _tastytradeApiClient.GetInstrumentFuture(futureTicker);

        return (future.Symbol, future.StreamerSymbol);
    }

    /// <summary>
    /// Converts a Lean ticker symbol to a brokerage-compatible format by replacing periods with slashes.
    /// </summary>
    /// <param name="leanTicker">The Lean ticker symbol to be converted.</param>
    /// <returns>The brokerage-compatible ticker symbol.</returns>
    /// <remarks>
    /// <para>This conversion is required because Lean uses dots (.) in symbols, 
    /// while brokerages use slashes (/).</para>
    /// <para><b>Example:</b></para>
    /// <list type="bullet">
    /// <item>
    /// <term>Lean</term>
    /// <description><c>Symbol.Create("BRK.B", SecurityType.Equity, Market.USA)</c></description>
    /// </item>
    /// <item>
    /// <term>Brokerage</term>
    /// <description><c>BRK/B</c></description>
    /// </item>
    /// </list>
    /// </remarks>
    private static string ToBrokerageTickerFormat(string leanTicker)
    {
        return leanTicker.Replace('.', '/');
    }

    /// <summary>
    /// Converts an option symbol to brokerage format by removing any periods.
    /// </summary>
    /// <param name="leanTicker">The Lean ticker for the option.</param>
    /// <returns>The brokerage-compatible ticker format for options.</returns>
    private static string ToOptionBrokerageTickerFormat(string leanTicker)
    {
        return leanTicker.Replace(".", string.Empty);
    }

    /// <summary>
    /// Converts an option symbol from brokerage format to Lean format by replacing slashes with periods.
    /// </summary>
    /// <param name="brokerageTicker">The brokerage-formatted ticker symbol.</param>
    /// <returns>The Lean-compatible ticker format with slashes replaced by periods.</returns>
    /// <remarks>
    /// This method is useful when receiving option tickers from brokerages that use slashes ('/') instead of periods ('.').
    /// </remarks>
    private static string ToOptionLeanTickerFormat(string brokerageTicker)
    {
        return brokerageTicker.Replace('/', '.');
    }

    /// <summary>
    /// Converts a futures ticker from brokerage format to Lean format by removing slashes.
    /// </summary>
    /// <param name="brokerageTicker">The brokerage-formatted ticker symbol for futures.</param>
    /// <returns>The Lean-compatible futures ticker without slashes.</returns>
    /// <remarks>
    /// <para>Futures tickers from brokerages often include slashes to separate contract components. 
    /// This method removes them for compatibility with Lean’s symbol parsing.</para>
    /// <para><b>Example:</b></para>
    /// <list type="bullet">
    /// <item>
    /// <term>Brokerage</term>
    /// <description><c>/ESU25</c></description>
    /// </item>
    /// <item>
    /// <term>Lean</term>
    /// <description><c>ESU25</c></description>
    /// </item>
    /// </list>
    /// </remarks>
    private static string ToFutureLeanTickerFormat(string brokerageTicker)
    {
        return brokerageTicker.Replace("/", "");
    }
}
