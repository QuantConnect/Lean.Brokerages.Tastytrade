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
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Orders;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.AccountData;

/// <summary>
/// Represents a response that contains account-related order data.
/// </summary>
public class AccountData
{
    /// <summary>
    /// Gets the type of the event. This is typically used in a <c>switch</c> statement
    /// to determine how the response should be handled.
    /// </summary>
    public EventType Type { get; set; }

    /// <summary>
    /// Gets the Unix timestamp (in milliseconds) indicating when the response was generated.
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets the order associated with the account event.
    /// </summary>
    [JsonProperty("data")]
    public Order Order { get; set; }
}
