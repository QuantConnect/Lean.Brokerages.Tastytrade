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

using QuantConnect.Brokerages.Authentication;

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Represents a request for an access token from Lean, containing required account and brokerage details.
/// </summary>
public sealed class LeanAccessTokenMetaDataRequest : AccessTokenMetaDataRequest
{
    /// <summary>
    /// Gets the refresh token used to request a new access token.
    /// </summary>
    public string RefreshToken { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LeanAccessTokenMetaDataRequest"/> class.
    /// </summary>
    /// <param name="brokerage">The brokerage name associated with the request.</param>
    /// <param name="refreshToken">The refresh token used to obtain a new access token.</param>
    /// <param name="accountNumber">The account number linked to the request.</param>
    public LeanAccessTokenMetaDataRequest(string brokerage, string refreshToken, string accountNumber)
        : base(brokerage, accountNumber)
    {
        RefreshToken = refreshToken;
    }
}
