﻿/*
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

namespace QuantConnect.Brokerages.Tastytrade.Serialization;

/// <summary>
/// Provides globally accessible instances of <see cref="JsonSerializerSettings"/> 
/// preconfigured with custom contract resolvers, such as kebab-case formatting.
/// </summary>
public static class JsonSettings
{
    /// <summary>
    /// Gets a reusable instance of <see cref="JsonSerializerSettings"/> that uses
    /// <see cref="KebabCaseContractResolver"/> for kebab-case property name formatting.
    /// </summary>
    public static readonly JsonSerializerSettings KebabCase = new()
    {
        ContractResolver = KebabCaseContractResolver.Instance,
        NullValueHandling = NullValueHandling.Ignore
    };

    /// <summary>
    /// Gets a reusable instance of <see cref="JsonSerializerSettings"/> that uses
    /// <see cref="CamelCaseContractResolver"/> for camelCase property name formatting.
    /// </summary>
    public static readonly JsonSerializerSettings CamelCase = new()
    {
        ContractResolver = CamelCaseContractResolver.Instance,
        NullValueHandling = NullValueHandling.Ignore
    };
}
