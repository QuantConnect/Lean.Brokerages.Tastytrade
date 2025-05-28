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

using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Represents a response that contains an error.
/// </summary>
public readonly struct ErrorResponse
{
    /// <summary>
    /// Gets the <see cref="Error"/> contained in the response.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorResponse"/> struct.
    /// </summary>
    /// <param name="error">The error to include in the response.</param>
    [JsonConstructor]
    public ErrorResponse(Error error) => Error = error;

    /// <summary>
    /// Returns a string that represents the current error response.
    /// </summary>
    /// <returns>A string describing the error in the response.</returns>
    public override string ToString()
    {
        return $"ErrorResponse: {Error}";
    }
}

/// <summary>
/// Represents a detailed error with a code, message, and optional nested errors.
/// </summary>
public readonly struct Error
{
    /// <summary>
    /// Gets the error code that identifies the type of error.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the descriptive message associated with the error.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets a collection of nested <see cref="Error"/> instances that provide additional context or detail.
    /// </summary>
    public IReadOnlyCollection<Error> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> struct.
    /// </summary>
    /// <param name="code">The unique code for the error.</param>
    /// <param name="message">The human-readable message describing the error.</param>
    /// <param name="errors">An optional collection of nested <see cref="Error"/> objects that give additional detail.</param>
    [JsonConstructor]
    public Error(string code, string message, IReadOnlyCollection<Error> errors) => (Code, Message, Errors) = (code, message, errors);

    /// <summary>
    /// Returns a string that represents the current error.
    /// </summary>
    /// <returns>A string combining the error code and message.</returns>
    public override string ToString()
    {
        var builder = new StringBuilder($"Error Code: {Code}, Message: {Message}");

        if (Errors?.Count > 0)
        {
            builder.AppendLine($", Nested Errors ({Errors.Count}):");
            var index = 1;
            foreach (var nested in Errors)
            {
                builder.AppendLine($"  {index++}. Error Code: {nested.Code}, Message: {nested.Message}");
            }
        }

        return builder.ToString().TrimEnd();
    }
}
