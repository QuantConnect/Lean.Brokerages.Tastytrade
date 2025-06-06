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

namespace QuantConnect.Brokerages.Tastytrade.Services;

/// <summary>
/// Provides level-one market data tracking for the best bid and ask prices.
/// </summary>
public sealed class LevelOneService : IOrderBookUpdater<decimal, decimal>
{
    /// <summary>
    /// Raised when the best bid or ask price changes.
    /// </summary>
    public event EventHandler<BestBidAskUpdatedEventArgs> BestBidAskUpdated;

    /// <summary>
    /// The trading symbol associated with this service.
    /// </summary>
    public Symbol Symbol { get; }

    /// <summary>
    /// Gets the best available bid price.
    /// </summary>
    public decimal BestBidPrice { get; private set; }

    /// <summary>
    /// Gets the size of the best available bid.
    /// </summary>
    public decimal BestBidSize { get; private set; }

    /// <summary>
    /// Gets the best available ask price.
    /// </summary>
    public decimal BestAskPrice { get; private set; }

    /// <summary>
    /// Gets the size of the best available ask.
    /// </summary>
    public decimal BestAskSize { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelOneService"/> class with the specified parameters.
    /// </summary>
    /// <param name="symbol">The trading symbol for this market data tracker.</param>
    public LevelOneService(Symbol symbol)
    {
        Symbol = symbol;
    }

    /// <summary>
    /// Removes an ask price level from the order book.
    /// </summary>
    /// <param name="price">The ask price level to remove.</param>
    public void RemoveAskRow(decimal price)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Removes a bid price level from the order book.
    /// </summary>
    /// <param name="price">The bid price level to remove.</param>
    public void RemoveBidRow(decimal price)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Updates the best ask price and size in the order book.
    /// </summary>
    /// <param name="price">The new best ask price.</param>
    /// <param name="size">The new best ask size.</param>
    public void UpdateAskRow(decimal price, decimal size)
    {
        BestAskPrice = price;
        BestAskSize = size;

        if (BestAskPrice == 0)
        {
            throw new ArgumentException($"{nameof(LevelOneService)}.{nameof(UpdateAskRow)}: Best ask price must be greater than zero.");
        }

        BestBidAskUpdated?.Invoke(this, new BestBidAskUpdatedEventArgs(Symbol, BestBidPrice, BestBidSize, BestAskPrice, BestAskSize));
    }

    /// <summary>
    /// Updates the best bid price and size in the order book.
    /// </summary>
    /// <param name="price">The new best bid price.</param>
    /// <param name="size">The new best bid size.</param>
    public void UpdateBidRow(decimal price, decimal size)
    {
        BestBidSize = size;
        BestBidPrice = price;

        if (BestBidPrice == 0)
        {
            throw new ArgumentException($"{nameof(LevelOneService)}.{nameof(UpdateBidRow)}: Best bid price must be greater than zero.");
        }

        BestBidAskUpdated?.Invoke(this, new BestBidAskUpdatedEventArgs(Symbol, BestBidPrice, BestBidSize, BestAskPrice, BestAskSize));
    }
}
