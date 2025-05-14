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

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Represents a generic response wrapper for API results, containing the response data and context information.
/// </summary>
/// <typeparam name="T">The type of the data contained in the response.</typeparam>
public class BaseResponse<T>
{
    /// <summary>
    /// Gets the data returned by the API.
    /// </summary>
    public T Data { get; }

    /// <summary>
    /// Gets additional context information about the API response.
    /// </summary>
    public string Context { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseResponse{T}"/> class with the specified data and context.
    /// </summary>
    /// <param name="data">The data returned by the API.</param>
    /// <param name="context">The context or metadata related to the response.</param>
    public BaseResponse(T data, string context) => (Data, Context) = (data, context);
}

