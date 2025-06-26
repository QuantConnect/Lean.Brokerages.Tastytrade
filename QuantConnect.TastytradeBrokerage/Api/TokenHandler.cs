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
using System.Net.Http;
using System.Threading;
using System.Net.Http.Headers;
using QuantConnect.Brokerages.Tastytrade.Models;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Api;

/// <summary>
/// Base class for implementing token-based authentication handlers in HTTP pipelines.
/// </summary>
public abstract class TokenHandler : DelegatingHandler, ITokenProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenHandler"/> class with a default <see cref="HttpClientHandler"/>.
    /// </summary>
    protected TokenHandler() : base(new HttpClientHandler())
    { }

    /// <summary>
    /// Retrieves a valid access token and its type for use in HTTP requests.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation while retrieving the access token.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description><c>TokenType</c>: The type of the token (e.g., "Bearer").</description></item>
    /// <item><description><c>AccessToken</c>: The access token string for authentication.</description></item>
    /// </list>
    /// </returns>
    public abstract (TokenType TokenType, string AccessToken) GetAccessToken(CancellationToken cancellationToken);

    /// <inheritdoc/>
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var (tokenType, accessToken) = GetAccessToken(cancellationToken);

        request.Headers.Authorization = tokenType switch
        {
            TokenType.SessionToken => new AuthenticationHeaderValue(accessToken),
            TokenType.Bearer => new AuthenticationHeaderValue(accessToken),
            _ => throw new NotSupportedException($"{nameof(TokenHandler)}.{nameof(Send)}: Token type '{tokenType}' is not supported.")
        };

        return base.Send(request, cancellationToken);
    }
}
