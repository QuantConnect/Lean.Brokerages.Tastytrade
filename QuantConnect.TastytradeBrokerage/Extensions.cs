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
using Newtonsoft.Json;
using QuantConnect.Securities;
using QuantConnect.Orders.TimeInForces;
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
    /// Gets the BrokerageTimeInForce and optional cancellation time based on the TimeInForce value.
    /// </summary>
    /// <param name="timeInForce">The TimeInForce value.</param>
    /// <returns>A tuple containing the Duration and optional expiry DateTime.</returns>
    public static (BrokerageTimeInForce TimeInForce, DateTime? ExpiryDateTime) GetBrokerageTimeInForceByLeanTimeInForce(this Orders.TimeInForce timeInForce)
    {
        var expiryDateTime = default(DateTime?); // Use nullable DateTime for clarity
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
    /// Converts an <see cref="InstrumentType"/> to a corresponding <see cref="SecurityType"/>.
    /// </summary>
    /// <param name="instrumentType">The instrument type to convert.</param>
    /// <param name="optionUnderlyingSymbol">An optional underlying symbol used for options.</param>
    /// <returns>The corresponding <see cref="SecurityType"/>.</returns>
    /// <exception cref="NotSupportedException">Thrown when the provided <paramref name="instrumentType"/> is not supported.</exception>
    public static SecurityType ConvertInstrumentTypeToSecurityType(this InstrumentType instrumentType, string optionUnderlyingSymbol = default) => instrumentType switch
    {
        // TODO: Add missed support types.
        InstrumentType.Equity => SecurityType.Equity,
        _ => throw new NotSupportedException($"{nameof(Extensions)}.{nameof(ConvertInstrumentTypeToSecurityType)}: The InstrumentType '{instrumentType}' is not supported.")
    };

    /// <summary>
    /// Determines whether the specified <see cref="OrderAction"/> represents a buy action.
    /// </summary>
    /// <param name="orderAction">The order action to evaluate.</param>
    /// <returns><c>true</c> if the action is a buy-type (e.g., Buy, BuyToOpen, BuyToClose); otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <paramref name="orderAction"/> is not a recognized buy or sell action.
    /// </exception>
    public static bool IsBuy(this OrderAction orderAction) => orderAction switch
    {
        OrderAction.Buy or OrderAction.BuyToOpen or OrderAction.BuyToClose => true,
        OrderAction.Sell or OrderAction.SellToClose or OrderAction.SellToOpen => false,
        _ => throw new ArgumentOutOfRangeException(nameof(orderAction), orderAction, "Unsupported order action")
    };
}
