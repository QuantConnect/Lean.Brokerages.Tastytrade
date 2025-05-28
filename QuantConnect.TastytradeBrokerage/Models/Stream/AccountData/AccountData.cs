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
using QuantConnect.Brokerages.Tastytrade.Models.Orders;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.AccountData;

/// <summary>
/// Represents a response that contains account-related order data.
/// </summary>
public sealed class AccountData : BaseResponse
{
    /// <summary>
    /// Gets the order associated with the account event.
    /// </summary>
    [JsonProperty("data")]
    public Order Order { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountData"/> class with the specified event type, timestamp, and order data.
    /// </summary>
    /// <param name="type">The type of the event.</param>
    /// <param name="timestamp">The event timestamp (in milliseconds since epoch).</param>
    /// <param name="order">The order data associated with the event.</param>
    [JsonConstructor]
    public AccountData(EventType type, long timestamp, Order order)
        : base(type, timestamp)
    {
        Order = order;
    }
}
