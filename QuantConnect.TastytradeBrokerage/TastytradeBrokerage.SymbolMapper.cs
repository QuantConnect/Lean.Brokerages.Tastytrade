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
using System.Globalization;
using QuantConnect.Securities;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.FutureOption;
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
    /// Provides access to specific properties for various symbols.
    /// </summary>
    private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase;

    /// <summary>
    /// A cache mapping Lean symbols to their corresponding brokerage and stream symbols.
    /// </summary>
    private static readonly ConcurrentDictionary<Symbol, BaseInstrument> _brokerageSymbolsByLeanSymbol = [];

    /// <summary>
    /// A cache mapping brokerage symbols to their corresponding Lean symbols.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Symbol> _leanSymbolByBrokerageSymbol = [];

    /// <summary>
    /// A cache mapping brokerage stream symbols to their corresponding Lean symbols.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Symbol> _leanSymbolByBrokerageStreamSymbol = [];

    /// <summary>
    /// Maps Lean future market identifiers to their corresponding streamer exchange codes used for subscribing to real-time market data.
    /// </summary>
    private readonly Dictionary<string, string> _futureLeanMarketToStreamExchange = new()
    {
        { Market.CFE, "XCBF" },
        { Market.CBOT, "XCBT" },
        { Market.CME, "XCME" },
        { Market.COMEX, "XCEC" },
        { Market.NYMEX, "XNYM" }

    };

    /// <summary>
    /// Represents a set of supported security types.
    /// </summary>
    /// <remarks>
    /// This HashSet contains the supported security types that are allowed within the system.
    /// </remarks>
    public readonly HashSet<SecurityType> SupportedSecurityType = new()
    {
        SecurityType.Equity,
        SecurityType.Option,
        SecurityType.Index,
        SecurityType.IndexOption,
        SecurityType.Future,
        SecurityType.FutureOption
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="TastytradeBrokerageSymbolMapper"/> class.
    /// </summary>
    /// <param name="tastytradeApiClient">The Tastytrade API client used to query future instruments.</param>
    public TastytradeBrokerageSymbolMapper(TastytradeApiClient tastytradeApiClient)
    {
        _tastytradeApiClient = tastytradeApiClient;
        _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
    }

    /// <summary>
    /// Converts a brokerage symbol into a Lean <see cref="Symbol"/> instance.
    /// </summary>
    /// <param name="brokerageSymbol">The full brokerage symbol representing the security.</param>
    /// <param name="securityType">The <see cref="SecurityType"/> of the instrument (e.g., Equity, Option, Future, FutureOption).</param>
    /// <param name="underlyingBrokerageSymbol">The brokerage symbol of the underlying instrument, used for options and future options.</param>
    /// <param name="isOptionIndex">Optional flag indicating if the option is on an index (true) or equity (false). If null, this is resolved dynamically.</param>
    /// <returns>A new <see cref="Symbol"/> instance representing the Lean symbol.</returns>
    /// <exception cref="ArgumentException">Thrown when option symbol decomposition fails.</exception>
    /// <exception cref="NotImplementedException">Thrown when the given security type is not supported.</exception>
    public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string underlyingBrokerageSymbol = default, bool? isOptionIndex = null)
    {
        brokerageSymbol = RemovePostfix(brokerageSymbol);
        if (_leanSymbolByBrokerageStreamSymbol.TryGetValue(brokerageSymbol, out var leanSymbol))
        {
            return leanSymbol;
        }
        else if (_leanSymbolByBrokerageSymbol.TryGetValue(brokerageSymbol, out leanSymbol))
        {
            return leanSymbol;
        }

        switch (securityType)
        {
            case SecurityType.Equity:
                leanSymbol = Symbol.Create(NormalizeEquityTicker(brokerageSymbol), securityType, Market.USA);
                break;
            case SecurityType.Future:
                leanSymbol = SymbolRepresentation.ParseFutureSymbol(ToFutureLeanTickerFormat(brokerageSymbol));
                break;
            case SecurityType.Option:
            case SecurityType.IndexOption:
                var isIndex = isOptionIndex ?? _tastytradeApiClient.IsUnderlyingEquityAnIndexAsync(underlyingBrokerageSymbol);

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

                    var underlyingSymbol = Symbol.Create(NormalizeEquityTicker(underlyingBrokerageSymbol), SecurityType.Equity, Market.USA);
                    leanSymbol = Symbol.CreateOption(underlyingSymbol, Market.USA, securityType.DefaultOptionStyle(), osiRight, osiStrike, osiExpiry);
                }
                break;
            case SecurityType.FutureOption:
                leanSymbol = ParseBrokerageFutureOptionSymbol(brokerageSymbol);
                break;
            default:
                throw new NotImplementedException($"{nameof(TastytradeBrokerageSymbolMapper)}.{nameof(GetLeanSymbol)}: " +
                    $"The security type '{securityType}' with brokerage symbol '{brokerageSymbol}' is not supported.");
        }

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
            return (brokerageSymbols.Symbol, brokerageSymbols.StreamerSymbol);
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
                (brokerageSymbol, brokerageStreamMarketDataSymbol) = GenerateFutureBrokerageSymbols(symbol);
                break;
            case SecurityType.FutureOption:
                (brokerageSymbol, brokerageStreamMarketDataSymbol) = GenerateFutureOptionBrokerageSymbols(symbol);
                break;
            default:
                throw new NotSupportedException($"{nameof(TastytradeBrokerageSymbolMapper)}.{nameof(GetBrokerageSymbols)}.");
        }

        _brokerageSymbolsByLeanSymbol[symbol] = new BaseInstrument()
        {
            Symbol = brokerageSymbol,
            StreamerSymbol = brokerageStreamMarketDataSymbol
        };
        _leanSymbolByBrokerageSymbol[brokerageSymbol] = symbol;
        _leanSymbolByBrokerageStreamSymbol[brokerageStreamMarketDataSymbol] = symbol;

        return (brokerageSymbol, brokerageStreamMarketDataSymbol);
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

        return ($"{underlying,-6}{expiryDate}{optionRight}{symbol.ID.StrikePrice * 1000m:00000000}", $".{underlying}{expiryDate}{optionRight}{symbol.ID.StrikePrice.ToTrimmedStringInvariant()}");
    }

    /// <summary>
    /// Generates the brokerage symbol and the streaming market data symbol for a given Lean future <see cref="Symbol"/>.
    /// </summary>
    /// <param name="symbol">The Lean future symbol (e.g., created via <c>Symbol.CreateFuture</c>).</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item><description>The brokerage symbol (e.g., <c>/6BU4</c>).</description></item>
    ///   <item><description>The streaming market data symbol (e.g., <c>/6BU24:XCME</c>).</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// This method constructs symbols by using the underlying code (e.g., "6B") and appending the appropriate
    /// futures month code and year suffix. The exchange code is looked up from <c>futuresSymbolToStreamExchange</c>.
    /// </remarks>
    private (string brokerageSymbol, string brokerageStreamMarketDataSymbol) GenerateFutureBrokerageSymbols(Symbol symbol)
    {
        var baseSymbol = "/" + SymbolRepresentation.GenerateFutureTicker(symbol.ID.Symbol, symbol.ID.Date, includeExpirationDate: false);
        return (baseSymbol.Remove(baseSymbol.Length - 2, 1), $"{baseSymbol}:{_futureLeanMarketToStreamExchange[symbol.ID.Market]}");
    }

    /// <summary>
    /// Generates the brokerage symbols for a given <see cref="Symbol"/> representing a future option in QuantConnect.
    /// </summary>
    /// <param name="symbol">
    /// The <see cref="Symbol"/> representing the future option for which to generate the brokerage symbols.
    /// </param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///     <c>brokerageSymbol</c>: The symbol used by the brokerage for order placement (e.g., "./GCG6 OGF6  251223P2075").
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     <c>brokerageStreamMarketDataSymbol</c>: The symbol used to subscribe to streaming market data 
    ///     from the brokerage (e.g., "./OGF26P2075:XCEC").
    ///     </description>
    ///   </item>
    /// </list>
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a required mapping (e.g., futures exchange symbol) cannot be found for the given symbol.
    /// </exception>
    private (string brokerageSymbol, string brokerageStreamMarketDataSymbol) GenerateFutureOptionBrokerageSymbols(Symbol symbol)
    {
        var futureOptionExpiryDate = symbol.ID.Date;

        var monthCode = SymbolRepresentation.FuturesMonthLookup[FuturesOptionsUnderlyingMapper.GetFutureContractMonthNoRulesApplied(symbol.Underlying.Canonical, futureOptionExpiryDate).Date.Month];

        var optionRoot = $"{symbol.ID.Symbol}{monthCode}";

        var optionRight = symbol.ID.OptionRight.ToString()[0];
        var yearSuffix = futureOptionExpiryDate.ToString("yy");

        var (underlyingFuture, _) = GenerateFutureBrokerageSymbols(symbol.Underlying);

        return ($".{underlyingFuture,-6}{optionRoot + yearSuffix.Last(),-6}{futureOptionExpiryDate.ToStringInvariant(DateFormat.SixCharacter)}{optionRight}{symbol.ID.StrikePrice.ToTrimmedStringInvariant()}",
            $"./{optionRoot + yearSuffix}{optionRight}{symbol.ID.StrikePrice.ToTrimmedStringInvariant()}:{_futureLeanMarketToStreamExchange[symbol.ID.Market]}");
    }

    /// <summary>
    /// Parses a brokerage-formatted future option symbol string into a <see cref="Symbol"/> object.
    /// </summary>
    /// <param name="brokerageSymbol">
    /// The future option symbol string in brokerage format, expected in the format: <c>&lt;FutureUnderlyingSymbol&gt; &lt;FutureOptionSymbol&gt; yyMMddP/CStrike</c>.
    /// For example: <c>"TT ZN 250819C126500"</c>, without space: <c>"./MNQZ5MNQZ5 251219P11000"</c>  
    /// </param>
    /// <returns>
    /// A <see cref="Symbol"/> object representing the parsed future option, including its underlying symbol,
    /// market, option type, strike price, and expiration date.
    /// </returns>
    /// <exception cref="FormatException">
    /// Thrown when the input string does not match the expected format.
    /// </exception>
    private Symbol ParseBrokerageFutureOptionSymbol(string brokerageSymbol)
    {
        // Ensure space after first 7 chars for parsing.
        // Examples: "./MNQZ5MNQZ5 251219P11000" or "./ZBU5 OZBN5 250620C142.5"
        if (!char.IsWhiteSpace(brokerageSymbol[6]))
        {
            brokerageSymbol = brokerageSymbol.Insert(7, " ");
        }

        var match = Regex.Match(brokerageSymbol, @"^\s*(\S+)\s+(\S+)\s+(\d{6})([PC])(\d+(?:\.\d+)?)$");

        if (!match.Success)
            throw new FormatException($"{nameof(TastytradeBrokerageSymbolMapper)}.{nameof(ParseBrokerageFutureOptionSymbol)}: Input '{brokerageSymbol}' is not in a valid option format (expected 'yyMMddP/CStrike').");

        var futureOptionTicker = SymbolRepresentation.ParseFutureTicker(ToFutureLeanTickerFormat(match.Groups[2].Value)).Underlying;
        var expiry = DateTime.ParseExact(match.Groups[3].Value, "yyMMdd", CultureInfo.InvariantCulture);
        var right = match.Groups[4].Value[0] == 'C' ? OptionRight.Call : OptionRight.Put;
        var strike = Convert.ToDecimal(match.Groups[5].Value);

        var futureTicker = FuturesOptionsSymbolMappings.MapFromOption(futureOptionTicker);
        if (!_symbolPropertiesDatabase.TryGetMarket(futureTicker, SecurityType.Future, out var market))
        {
            throw new NotSupportedException($"No market found for future ticker '{futureTicker}' (derived from brokerage future option symbol '{brokerageSymbol}').");
        }

        try
        {
            var underylingSymbol = FuturesOptionsUnderlyingMapper.GetUnderlyingFutureFromFutureOption(futureOptionTicker, market, expiry, DateTime.Now);
            return Symbol.CreateOption(underylingSymbol, underylingSymbol.ID.Market, SecurityType.FutureOption.DefaultOptionStyle(), right, strike, expiry);
        }
        catch (Exception ex)
        {
            throw new NotSupportedException($"Failed to create Lean FutureOption Symbol from '{brokerageSymbol}'.", ex);
        }
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
    /// Normalizes a brokerage-formatted equity ticker for use with Lean by replacing slashes ('/') with periods ('.').
    /// </summary>
    /// <param name="brokerageTicker">The raw equity ticker symbol received from the brokerage (e.g., "BRK/K").</param>
    /// <returns>
    /// A Lean-compatible equity ticker with standardized formatting (e.g., "BRK.K").
    /// </returns>
    /// <remarks>
    /// This method is intended specifically for equity ticker symbols, where brokerages may use slashes to denote class shares or sub-symbols.
    /// Lean requires periods for these cases.
    /// </remarks>
    private static string NormalizeEquityTicker(string brokerageTicker)
    {
        return brokerageTicker.Replace('/', '.');
    }

    /// <summary>
    /// Converts a brokerage-formatted futures ticker into a Lean-compatible format by removing leading slashes or dots.
    /// </summary>
    /// <param name="brokerageTicker">The futures ticker provided by the brokerage (e.g., <c>/ESU25</c> or <c>./ESU25</c>).</param>
    /// <returns>The Lean-compatible ticker string with slashes and dots removed (e.g., <c>ESU25</c>).</returns>
    /// <remarks>
    /// <para>
    /// Brokerages may prefix futures tickers with characters like '/' or './'. These are removed to conform with 
    /// Lean’s expected format.
    /// </para>
    /// <para><b>Examples:</b></para>
    /// <list type="bullet">
    /// <item><description><c>/ESU25</c> → <c>ESU25</c></description></item>
    /// <item><description><c>./ESU25</c> → <c>ESU25</c></description></item>
    /// </list>
    /// </remarks>
    private static string ToFutureLeanTickerFormat(string brokerageTicker)
    {
        return brokerageTicker.Replace("/", string.Empty).Replace(".", string.Empty);
    }

    /// <summary>
    /// Removes the trailing curly-brace postfix from a brokerage symbol if present.
    /// <para>
    /// Candle updates from the websocket typically include a postfix in the format <c>{...}</c> 
    /// to indicate resolution or period (e.g., <c>AAPL{=d}</c>, <c>.AAPL2025{=h}</c>).
    /// </para>
    /// <para>
    /// Other websocket channels such as quote or trade feeds return symbols without the curly-brace postfix.
    /// This method normalizes the symbol by stripping the postfix for consistent handling.
    /// </para>
    /// </summary>
    /// <param name="symbol">The brokerage-formatted symbol received from the websocket feed.</param>
    /// <returns>The normalized symbol with the curly-brace postfix removed, if present.</returns>
    private static string RemovePostfix(string symbol) => symbol.Split('{')[0];
}
