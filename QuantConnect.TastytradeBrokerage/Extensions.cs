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
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;
using QuantConnect.Orders;
using System.Globalization;
using QuantConnect.Securities;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Brokerages.Tastytrade.Models;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Serialization;
using BrokerageTimeInForce = QuantConnect.Brokerages.Tastytrade.Models.Enum.TimeInForce;

namespace QuantConnect.Brokerages.Tastytrade;

/// <summary>
/// Provides extension methods.
/// </summary
public static class Extensions
{
    /// <summary>
    /// Deserializes the specified JSON string to an object of type <typeparamref name="T"/>
    /// using kebab-case property name resolution.
    /// </summary>
    /// <typeparam name="T">The target type of the deserialized object.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
    public static T DeserializeKebabCase<T>(this string json)
    {
        return JsonConvert.DeserializeObject<T>(json, JsonSettings.KebabCase);
    }

    /// <summary>
    /// Deserializes the specified JSON string to an object of type <typeparamref name="T"/>
    /// using camelCase property name resolution.
    /// </summary>
    /// <typeparam name="T">The target type of the deserialized object.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
    public static T DeserializeCamelCase<T>(this string json)
    {
        return JsonConvert.DeserializeObject<T>(json, JsonSettings.CamelCase);
    }

    /// <summary>
    /// Retrieves the time zone of the exchange for the given symbol.
    /// </summary>
    /// <param name="symbol">The symbol for which to get the exchange time zone.</param>
    /// <returns>
    /// The <see cref="NodaTime.DateTimeZone"/> representing the time zone of the exchange
    /// where the given symbol is traded.
    /// </returns>
    /// <remarks>
    /// This method uses the <see cref="MarketHoursDatabase"/> to fetch the exchange hours
    /// and extract the time zone information for the provided symbol.
    /// </remarks>
    public static NodaTime.DateTimeZone GetSymbolExchangeTimeZone(this Symbol symbol)
        => MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;

    /// <summary>
    /// Converts a Lean <see cref="Orders.TimeInForce"/> to a corresponding <see cref="BrokerageTimeInForce"/> value 
    /// and optionally returns the expiry <see cref="DateTime"/> if applicable.
    /// </summary>
    /// <param name="timeInForce">The Lean <see cref="Orders.TimeInForce"/> value.</param>
    /// <returns>
    /// A tuple containing the <see cref="BrokerageTimeInForce"/> and an optional expiry <see cref="DateTime"/>.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when the specified <paramref name="timeInForce"/> is not supported.</exception>
    public static (BrokerageTimeInForce TimeInForce, DateTime? ExpiryDateTime) GetBrokerageTimeInForceByLeanTimeInForce(this Orders.TimeInForce timeInForce)
    {
        var expiryDateTime = default(DateTime?);
        var duration = default(BrokerageTimeInForce);
        switch (timeInForce)
        {
            case DayTimeInForce:
                duration = BrokerageTimeInForce.Day;
                break;
            case GoodTilCanceledTimeInForce:
                duration = BrokerageTimeInForce.GoodTillCancel;
                break;
            case GoodTilDateTimeInForce goodTilDateTime:
                duration = BrokerageTimeInForce.GoodTilDate;
                expiryDateTime = goodTilDateTime.Expiry;
                break;
            default:
                throw new NotSupportedException($"{nameof(Extensions)}.{nameof(GetBrokerageTimeInForceByLeanTimeInForce)}: The TimeInForce '{timeInForce}' is not supported.");
        }
        return (duration, expiryDateTime);
    }

    /// <summary>
    /// Tries to assign a Lean <see cref="Orders.TimeInForce"/> to <see cref="OrderProperties"/> based on a <see cref="BrokerageTimeInForce"/> value and optional expiration date.
    /// </summary>
    /// <param name="orderProperties">The <see cref="OrderProperties"/> to update.</param>
    /// <param name="timeInForce">The brokerage <see cref="BrokerageTimeInForce"/> value.</param>
    /// <param name="goodTilDateTime">The expiration <see cref="DateTime"/> for GoodTilDate orders.</param>
    /// <returns><c>true</c> if the mapping was successful; otherwise, <c>false</c>.</returns>
    public static bool TryGetLeanTimeInForce(this OrderProperties orderProperties, BrokerageTimeInForce timeInForce, DateTime goodTilDateTime)
    {
        switch (timeInForce)
        {
            case BrokerageTimeInForce.GoodTillCancel:
                orderProperties.TimeInForce = Orders.TimeInForce.GoodTilCanceled;
                return true;
            case BrokerageTimeInForce.Day:
                orderProperties.TimeInForce = Orders.TimeInForce.Day;
                return true;
            case BrokerageTimeInForce.GoodTilDate:
                orderProperties.TimeInForce = Orders.TimeInForce.GoodTilDate(goodTilDateTime);
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Converts a Lean <see cref="SecurityType"/> to the corresponding brokerage-specific <see cref="InstrumentType"/>.
    /// </summary>
    /// <param name="securityType">The Lean <see cref="SecurityType"/> to convert.</param>
    /// <returns>The equivalent <see cref="InstrumentType"/> used by the brokerage.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specified <paramref name="securityType"/> does not have a corresponding brokerage instrument type mapping.
    /// </exception>
    public static InstrumentType ConvertLeanSecurityTypeToBrokerageInstrumentType(this SecurityType securityType)
    {
        return securityType switch
        {
            SecurityType.Equity => InstrumentType.Equity,
            SecurityType.Option or SecurityType.IndexOption => InstrumentType.EquityOption,
            SecurityType.Future => InstrumentType.Future,
            SecurityType.FutureOption => InstrumentType.FutureOption,
            _ => throw new NotSupportedException($"{nameof(Extensions)}.{nameof(ConvertLeanSecurityTypeToBrokerageInstrumentType)}: No mapping exists for security type '{securityType}'."),
        };
    }

    /// <summary>
    /// Converts the specified <see cref="InstrumentType"/> to its corresponding <see cref="SecurityType"/>.
    /// </summary>
    /// <param name="instrumentType">The instrument type to convert.</param>
    /// <param name="isIndex">
    /// A value indicating whether the underlying equity is an index.
    /// This is used to differentiate between standard options and index options.
    /// </param>
    /// <returns>
    /// The corresponding <see cref="SecurityType"/> for the given <paramref name="instrumentType"/>.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specified <paramref name="instrumentType"/> is not supported by this conversion.
    /// </exception>
    public static SecurityType ConvertInstrumentTypeToSecurityType(this InstrumentType instrumentType) => instrumentType switch
    {
        InstrumentType.Equity => SecurityType.Equity,
        InstrumentType.EquityOption => SecurityType.Option,
        InstrumentType.Future => SecurityType.Future,
        InstrumentType.FutureOption => SecurityType.FutureOption,
        _ => throw new NotSupportedException($"{nameof(Extensions)}.{nameof(ConvertInstrumentTypeToSecurityType)}: The InstrumentType '{instrumentType}' is not supported.")
    };

    /// <summary>
    /// Converts the given quantity to a signed value based on the order action.
    /// Positive for buy actions, negative for sell actions.
    /// </summary>
    /// <param name="orderAction">The order action to evaluate.</param>
    /// <param name="quantity">The quantity to convert to a signed value.</param>
    /// <returns>
    /// A signed decimal: positive if the action is a buy, negative if it is a sell.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="orderAction"/> is not supported for signed quantity conversion.
    /// </exception>
    public static decimal ToSignedQuantity(this OrderAction orderAction, decimal quantity) => orderAction switch
    {
        OrderAction.Buy or OrderAction.BuyToOpen or OrderAction.BuyToClose => quantity,
        OrderAction.Sell or OrderAction.SellToClose or OrderAction.SellToOpen => decimal.Negate(quantity),
        _ => throw new ArgumentOutOfRangeException(nameof(orderAction), orderAction, $"Unsupported order action '{orderAction}' for signed quantity conversion.")
    };

    /// <summary>
    /// Encodes special characters in a symbol to make it URL-safe.
    /// Specifically replaces slashes (/) with their URL-encoded form (%2f).
    /// </summary>
    /// <param name="symbol">The symbol string to encode (e.g., "BRK/B").</param>
    /// <returns>The URL-encoded symbol (e.g., "BRK%2fB").</returns>
    /// <example>
    /// Example usage:
    /// <code>
    /// string encoded = "BRK/B".UrlEncodeSymbol();
    /// // encoded == "BRK%2fB"
    /// </code>
    /// </example>
    public static string UrlEncodeSymbol(this string symbol)
    {
        return symbol?.Replace("/", "%2f") ?? string.Empty;
    }

    /// <summary>
    /// Converts the decimal value to a string using the invariant culture and trims any trailing zeros.
    /// Uses the "G29" format specifier to preserve up to 29 significant digits without scientific notation unless necessary.
    /// </summary>
    /// <param name="value">The decimal value to convert.</param>
    /// <returns>
    /// A string representation of the decimal value without trailing zeros and using "." as the decimal separator.
    /// </returns>
    /// <example>
    /// <code>
    /// decimal value1 = 123.45000m;
    /// string result1 = value1.ToTrimmedStringInvariant(); // "123.45"
    /// 
    /// decimal value2 = 100.0000m;
    /// string result2 = value2.ToTrimmedStringInvariant(); // "100"
    /// </code>
    /// </example>
    public static string ToTrimmedStringInvariant(this decimal value)
    {
        return value.ToString("G29", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Synchronously reads and returns the content of an HTTP response as a string, optionally using a cancellation token.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponseMessage"/> whose content will be read.</param>
    /// <param name="cancellationToken">An optional token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response body content as a <see cref="string"/>.</returns>
    public static string ReadContentAsString(this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response?.Content == null)
        {
            return string.Empty;
        }

        return response.Content.ReadAsStringAsync(cancellationToken).SynchronouslyAwaitTaskResult();
    }

    /// <summary>
    /// Ensures that the HTTP response has a successful status code. 
    /// If not, it attempts to extract and parse the error message from the response content.
    /// </summary>
    /// <param name="responseMessage">The <see cref="HttpResponseMessage"/> received from the HTTP request.</param>
    /// <param name="requestMessage">The original <see cref="HttpRequestMessage"/> that was sent.</param>
    /// <param name="jsonBody">The serialized JSON body sent with the request.</param>
    /// <exception cref="HttpRequestException">
    /// Thrown when the response does not indicate success. 
    /// The exception message includes parsed error details (if available), request URI, HTTP method, and body.
    /// </exception>
    public static void EnsureSuccessStatusCode(this HttpResponseMessage responseMessage, HttpRequestMessage requestMessage, string jsonBody)
    {
        if (!responseMessage.IsSuccessStatusCode)
        {
            var response = responseMessage.ReadContentAsString();
            var error = default(string);
            try
            {
                error = response.DeserializeKebabCase<ErrorResponse>().Error.ToString();
            }
            catch
            {
                error = response;
            }

            var message = $"{error}, RequestUri: [{requestMessage.Method.Method}] {requestMessage.RequestUri}, Body: {jsonBody}";

            throw new HttpRequestException(message, null, responseMessage.StatusCode);
        }
    }

    /// <summary>
    /// Returns the symbol combined with the period type postfix based on the specified <see cref="Resolution"/>.
    /// </summary>
    /// <param name="resolution">The resolution that determines the period type postfix.</param>
    /// <param name="symbol">The base symbol to which the period type will be appended.</param>
    /// <returns>The symbol with the corresponding period type postfix (e.g., "AAPL{=1m}").</returns>
    /// <exception cref="NotSupportedException">Thrown when the provided resolution is not supported.</exception>
    public static string GetSymbolWithPeriodPostfix(this Resolution resolution, string symbol)
    {
        var periodPostfix = resolution switch
        {
            Resolution.Tick => "{=t}",
            Resolution.Second => "{=s}",
            Resolution.Minute => "{=m}",
            Resolution.Hour => "{=h}",
            Resolution.Daily => "{=d}",
            _ => throw new NotSupportedException($"The resolution '{resolution}' is not supported for symbol period formatting.")
        };

        return $"{symbol}{periodPostfix}";
    }
}
