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
using QuantConnect.Brokerages.Tastytrade.Serialization;

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
}
