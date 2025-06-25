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

using System.Threading;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Defines a contract for providing access tokens for authenticated HTTP requests.
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    /// Retrieves a valid access token and its type for authenticated HTTP requests.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation while retrieving the access token.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description><c>TokenType</c>: The type of the token (e.g., "Bearer").</description></item>
    /// <item><description><c>AccessToken</c>: The access token string for authentication.</description></item>
    /// </list>
    /// </returns>
    (TokenType TokenType, string AccessToken) GetAccessToken(CancellationToken cancellationToken);
}
