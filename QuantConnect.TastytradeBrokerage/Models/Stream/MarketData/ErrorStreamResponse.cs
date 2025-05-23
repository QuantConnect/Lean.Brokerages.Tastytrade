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
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

/// <summary>
/// Represents an error message received from a streaming API response.
/// </summary>
public readonly struct ErrorStreamResponse
{
    /// <summary>
    /// Gets the type of the event. For error responses, this is typically "ERROR".
    /// </summary>
    public EventType Type { get; }

    /// <summary>
    /// Gets the numeric identifier for the channel where the error occurred.
    /// A value of 0 may indicate a general or protocol-level error.
    /// </summary>
    public int Channel { get; }

    /// <summary>
    /// Gets the error code or identifier that categorizes the type of error.
    /// For example, "BAD_ACTION" indicates an invalid operation or protocol violation.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Gets the human-readable description of the error, providing more context or explanation.
    /// For example, "Protocol violation with an even channel usage."
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorStreamResponse"/> struct with detailed error information.
    /// </summary>
    /// <param name="type">The type of the event, typically "ERROR" for error responses.</param>
    /// <param name="channel">The channel number associated with the error.</param>
    /// <param name="error">The error code or category.</param>
    /// <param name="message">The detailed message describing the error.</param>
    [JsonConstructor]
    public ErrorStreamResponse(EventType type, int channel, string error, string message)
    {
        Type = type;
        Channel = channel;
        Error = error;
        Message = message;
    }

    /// <summary>
    /// Returns a string that provides a human-readable representation of the error.
    /// </summary>
    /// <returns>A string representation of the error stream response.</returns>
    public override string ToString()
    {
        return $"Type: {Type}, Channel: {Channel}, Error: {Error}, Message: {Message}";
    }
}
