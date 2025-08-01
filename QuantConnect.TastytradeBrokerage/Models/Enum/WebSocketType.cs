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

namespace QuantConnect.Brokerages.Tastytrade.Models.Enum;

/// <summary>
/// Specifies the type of WebSocket connection.
/// </summary>
public enum WebSocketType
{
    /// <summary>
    /// WebSocket used for receiving account-related updates.
    /// </summary>
    Account = 0,

    /// <summary>
    /// WebSocket used for receiving market data updates.
    /// </summary>
    MarketData = 1
}
