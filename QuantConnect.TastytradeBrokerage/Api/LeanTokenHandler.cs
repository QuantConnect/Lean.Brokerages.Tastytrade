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
using RestSharp;
using QuantConnect.Api;
using System.Threading;
using QuantConnect.Brokerages.Tastytrade.Models;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Api;

/// <summary>
/// Provides a token handler for retrieving and caching Lean platform access tokens.
/// </summary>
public class LeanTokenHandler : TokenHandler
{
    /// <summary>
    /// Stores metadata about the Lean access token and its expiration details.
    /// </summary>
    private LeanAccessTokenMetaDataResponse _accessTokenMetaData;

    /// <summary>
    /// API client for communicating with the Lean platform.
    /// </summary>
    private readonly ApiConnection _leanApiClient;

    /// <summary>
    /// The JSON body request used for API metadata operations.
    /// </summary>
    private readonly string _jsonBodyRequest;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeanTokenHandler"/> class.
    /// </summary>
    /// <param name="leanApiClient">The Lean API client instance.</param>
    /// <param name="brokerageName">The name of the brokerage requesting the token.</param>
    /// <param name="accountNumber">The associated account number.</param>
    /// <param name="refreshToken">The refresh token used to request a new access token.</param>
    public LeanTokenHandler(ApiConnection leanApiClient, string brokerageName, string accountNumber, string refreshToken) : base()
    {
        _leanApiClient = leanApiClient;
        _jsonBodyRequest = new LeanAccessTokenMetaDataRequest(brokerageName, refreshToken, accountNumber).ToJson();
    }

    /// <inheritdoc/>
    public override (TokenType, string) GetAccessToken(CancellationToken cancellationToken)
    {
        if (_accessTokenMetaData != null && DateTime.UtcNow < _accessTokenMetaData?.AccessTokenExpires)
        {
            return (_accessTokenMetaData.TokenType, _accessTokenMetaData.AccessToken);
        }

        try
        {
            var request = new RestRequest("live/auth0/refresh", Method.POST);
            request.AddJsonBody(_jsonBodyRequest);

            if (_leanApiClient.TryRequest<LeanAccessTokenMetaDataResponse>(request, out var response))
            {
                if (response.Success && !string.IsNullOrEmpty(response.AccessToken))
                {
                    _accessTokenMetaData = response;
                    return (response.TokenType, response.AccessToken);
                }
            }

            throw new InvalidOperationException(string.Join(",", response.Errors));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{nameof(LeanTokenHandler)}.{nameof(GetAccessToken)}: {ex.Message}, RequestBody = {_jsonBodyRequest}");
        }
    }
}
