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
/// Represents the brokerage symbol pair used for trading and streaming data.
/// </summary>
public readonly struct BrokerageSymbols
{
    /// <summary>
    /// Gets the symbol used when placing orders with the brokerage.
    /// </summary>
    public string BrokerageSymbol { get; }

    /// <summary>
    /// Gets the symbol used for subscribing to market data from the brokerage.
    /// </summary>
    public string BrokerageStreamMarketDataSymbol { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BrokerageSymbols"/> struct.
    /// </summary>
    /// <param name="brokerageSymbol">The symbol for placing trades.</param>
    /// <param name="brokerageStreamMarketDataSymbol">The symbol used for streaming market data.</param>
    public BrokerageSymbols(string brokerageSymbol, string brokerageStreamMarketDataSymbol)
        => (BrokerageSymbol, BrokerageStreamMarketDataSymbol) = (brokerageSymbol, brokerageStreamMarketDataSymbol);
}
