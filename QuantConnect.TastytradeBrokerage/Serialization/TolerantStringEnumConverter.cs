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
using QuantConnect.Logging;
using Newtonsoft.Json.Converters;
using System.Collections.Concurrent;

namespace QuantConnect.Brokerages.Tastytrade.Serialization;

/// <summary>
/// A <see cref="StringEnumConverter"/> that degrades to an <c>Unknown</c> member instead of throwing
/// when the brokerage sends an enum value this library does not know about yet.
/// </summary>
/// <remarks>
/// A single unrecognized value would otherwise fail the whole response and, for open orders, abort algorithm
/// initialization. The unrecognized value is logged verbatim so it can be added to the enum afterwards.
/// </remarks>
public class TolerantStringEnumConverter : StringEnumConverter
{
    /// <summary>
    /// The name every enum using this converter must define to receive unrecognized values.
    /// </summary>
    public const string UnknownMemberName = "Unknown";

    /// <summary>
    /// Tracks the (enum type, raw value) pairs already logged, so a response carrying many
    /// items with the same unrecognized value produces a single log entry.
    /// </summary>
    private static readonly ConcurrentDictionary<(Type, string), byte> _loggedUnknownValues = [];

    /// <summary>
    /// Reads the JSON representation of the enum, falling back to the <c>Unknown</c> member when the value is not recognized.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
    /// <param name="objectType">The target enum type, optionally nullable.</param>
    /// <param name="existingValue">The existing value of the object being read.</param>
    /// <param name="serializer">The calling <see cref="JsonSerializer"/>.</param>
    /// <returns>The parsed enum value, or the <c>Unknown</c> member when the value is not recognized.</returns>
    /// <exception cref="JsonSerializationException">
    /// Thrown when the value is not recognized and the target enum does not define an <c>Unknown</c> member.
    /// </exception>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // The raw token has to be captured up front: the base converter consumes it before it throws.
        var rawValue = reader.TokenType == JsonToken.String ? reader.Value?.ToString() : null;

        try
        {
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
        catch (JsonSerializationException) when (rawValue != null)
        {
            var enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;

            if (!Enum.IsDefined(enumType, UnknownMemberName))
            {
                throw;
            }

            if (_loggedUnknownValues.TryAdd((enumType, rawValue), default))
            {
                Log.Error($"{nameof(TolerantStringEnumConverter)}.{nameof(ReadJson)}: Unrecognized {enumType.Name} value '{rawValue}' received from the brokerage, using '{UnknownMemberName}'. Please report it so the value can be supported.");
            }

            return Enum.Parse(enumType, UnknownMemberName);
        }
    }
}
