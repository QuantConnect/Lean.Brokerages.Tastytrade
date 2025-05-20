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
using QuantConnect.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Models;

namespace QuantConnect.Brokerages.Tastytrade.Api;

/// <summary>
/// Provides methods for interacting with the Tastytrade API.
/// </summary>
public sealed class TastytradeApiClient
{
    /// <summary>
    /// The base URL of the Tastytrade API.
    /// </summary>
    private readonly string _baseUrl;

    /// <summary>
    /// The account number associated with the Tastytrade account.
    /// </summary>
    private readonly string _accountNumber;

    /// <summary>
    /// The HTTP client used to send requests to the Tastytrade API.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// A delegate that retrieves the current session token for authenticating API requests.
    /// </summary>
    public readonly Func<CancellationToken, Task<string>> GetSessionToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="TastytradeApiClient"/> class.
    /// </summary>
    /// <param name="baseUrl">The base URL of the Tastytrade API.</param>
    /// <param name="username">The Tastytrade account username.</param>
    /// <param name="password">The Tastytrade account password.</param>
    /// <param name="accountNumber">The account number associated with the Tastytrade account.</param>
    public TastytradeApiClient(string baseUrl, string username, string password, string accountNumber)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        var httpTokenHandler = new HttpTokenHandler(baseUrl, username, password);
        GetSessionToken = httpTokenHandler.GetSessionToken;
        _httpClient = new HttpClient(httpTokenHandler);
    }

    /// <summary>
    /// Retrieves the account balances for the associated Tastytrade account.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the account balances.</returns>
    public async Task<AccountBalance> GetAccountBalances()
    {
        return (await SendRequestAsync<AccountBalance>(HttpMethod.Get, $"/accounts/{_accountNumber}/balances")).Data;
    }

    /// <summary>
    /// Retrieves the current open positions for the associated Tastytrade account.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of positions.</returns>
    public async Task<IEnumerable<Position>> GetAccountPositions()
    {
        return (await SendRequestAsync<ResponseList<Position>>(HttpMethod.Get, $"/accounts/{_accountNumber}/positions")).Data.Items;
    }

    /// <summary>
    /// Retrieves the current API quote token from Tastytrade.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the API quote token.</returns>
    public async Task<ApiQuoteTokenResponse> GetApiQuoteToken()
    {
        return (await SendRequestAsync<ApiQuoteTokenResponse>(HttpMethod.Get, "/api-quote-tokens")).Data;
    }

    /// <summary>
    /// Sends an HTTP request and parses the response from the Tastytrade API.
    /// </summary>
    /// <typeparam name="T">The type of the expected response data.</typeparam>
    /// <param name="httpMethod">The HTTP method to use (e.g., GET, POST).</param>
    /// <param name="endpoint">The API endpoint relative to the base URL.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the parsed API response.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API response indicates a failure.</exception>
    /// <exception cref="Exception">Thrown when an unexpected error occurs while sending the request.</exception>
    private async Task<BaseResponse<T>> SendRequestAsync<T>(HttpMethod httpMethod, string endpoint)
    {
        using (var requestMessage = new HttpRequestMessage(httpMethod, _baseUrl + endpoint))
        {
            try
            {
                var responseMessage = await _httpClient.SendAsync(requestMessage);

                var response = await responseMessage.Content.ReadAsStringAsync();

                if (!responseMessage.IsSuccessStatusCode)
                {
                    var error = response.DeserializeKebabCase<ErrorResponse>().Error;
                    throw new HttpRequestException(error.ToString(), null, responseMessage.StatusCode);
                }

                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"{nameof(TastytradeApiClient)}:{nameof(SendRequestAsync)}.Response: {response}");
                }

                return response.DeserializeKebabCase<BaseResponse<T>>();
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(TastytradeApiClient)}.{nameof(SendRequestAsync)}: Unexpected error while sending request - {ex.Message}", ex);
            }
        }
    }
}
