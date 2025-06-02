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
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading;
using QuantConnect.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Models;
using QuantConnect.Brokerages.Tastytrade.Models.Orders;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

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
    public readonly string AccountNumber;

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
        AccountNumber = accountNumber;
    }

    /// <summary>
    /// Retrieves the account balances for the associated Tastytrade account.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the account balances.</returns>
    public async Task<AccountBalance> GetAccountBalances()
    {
        return (await SendRequestAsync<AccountBalance>(HttpMethod.Get, $"/accounts/{AccountNumber}/balances")).Data;
    }

    /// <summary>
    /// Retrieves the current open positions for the associated Tastytrade account.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of positions.</returns>
    public async Task<IReadOnlyCollection<Position>> GetAccountPositions()
    {
        return (await SendRequestAsync<ResponseList<Position>>(HttpMethod.Get, $"/accounts/{AccountNumber}/positions")).Data.Items;
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
    /// Retrieves an outright future instrument by its ticker symbol.
    /// </summary>
    /// <param name="futureTicker">The symbol of the future (e.g., "/ESM5").</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="Future"/> 
    /// representing basic metadata of the requested future.
    /// </returns>
    public async Task<Future> GetInstrumentFuture(string futureTicker)
    {
        return (await SendRequestAsync<Future>(HttpMethod.Get, "/instruments/futures/" + futureTicker)).Data;
    }

    /// <summary>
    /// Submits a brokerage order to the specified account.
    /// </summary>
    /// <param name="order">The brokerage order to be submitted.</param>
    /// <returns>
    /// The task result contains the <see cref="OrderResponse"/> returned by the brokerage API.
    /// </returns>
    public async Task<OrderResponse> SubmitOrder(OrderBaseRequest order)
    {
        return (await SendRequestAsync<OrderResponse>(HttpMethod.Post, $"/accounts/{AccountNumber}/orders", order.ToJson())).Data;
    }

    /// <summary>
    /// Retrieves all live orders for the account, including those with statuses:
    /// Received, Routed, InFlight, and Live.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains 
    /// a read-only collection of <see cref="Order"/> objects representing live orders.
    /// </returns>
    /// <remarks>
    /// The query includes multiple order statuses and limits the result to 100 items per page.
    /// </remarks>
    public async Task<IReadOnlyCollection<Order>> GetLiveOrders()
    {
        var query = $"status[]={OrderStatus.Received}&status[]={OrderStatus.Routed}&status[]={OrderStatus.InFlight}&status[]={OrderStatus.Live}&per-page=100";
        return (await SendRequestAsync<ResponseList<Order>>(HttpMethod.Get, $"/accounts/{AccountNumber}/orders?{query}")).Data.Items;
    }

    /// <summary>
    /// Cancels an order by its unique identifier.
    /// </summary>
    /// <param name="id">The unique ID of the order to be canceled.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// This method issues a DELETE request to the brokerage API to cancel the order associated with the specified ID.
    /// </remarks>
    public async Task CancelOrderById(string id)
    {
        await SendRequestAsync<object>(HttpMethod.Delete, $"/accounts/{AccountNumber}/orders/{id}");
    }

    /// <summary>
    /// Determines whether the specified symbol corresponds to an index, based on its underlying equity definition.
    /// </summary>
    /// <param name="symbol">The equity option symbol to check.</param>
    /// <returns>
    /// The task result contains <c>true</c> if the underlying equity is an index; otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> IsUnderlyingEquityAnIndexAsync(string symbol)
    {
        return (await SendRequestAsync<Equity>(HttpMethod.Get, $"/instruments/equities/{symbol.UrlEncodeSymbol()}")).Data.IsIndex;
    }

    /// <summary>
    /// Retrieves a specific future option from the option chains for the given underlying future symbol.
    /// </summary>
    /// <param name="underlyingFutureTicker">The ticker symbol of the underlying future.</param>
    /// <param name="expirationDate">The expiration date of the desired option.</param>
    /// <param name="strike">The strike price of the desired option.</param>
    /// <param name="optionType">The type of the option (e.g., Call or Put).</param>
    /// <returns>
    /// A <see cref="FutureOption"/> object that matches the specified expiration date, strike price, and option type.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no matching future option is found in the option chains for the given parameters.
    /// </exception>
    public async Task<FutureOption> GetFutureOptionChains(string underlyingFutureTicker, DateTime expirationDate, decimal strike, OptionType optionType)
    {
        var futureOptions = (await SendRequestAsync<ResponseList<FutureOption>>(HttpMethod.Get, $"/futures-option-chains/{underlyingFutureTicker}")).Data.Items;
        var matchingOption = futureOptions.FirstOrDefault(fo => fo.IsMatchFor(expirationDate, strike, optionType));

        if (matchingOption.Equals(default(FutureOption)))
        {
            throw new InvalidOperationException($"{nameof(TastytradeApiClient)}.{nameof(GetFutureOptionChains)}: No matching future option found for ticker '{underlyingFutureTicker}', " +
                                                $"expiration '{expirationDate:yyyy-MM-dd}', strike '{strike}', type '{optionType}'.");
        }

        return matchingOption;
    }

    /// <summary>
    /// Sends an HTTP request and parses the response from the Tastytrade API.
    /// </summary>
    /// <typeparam name="T">The type of the expected response data.</typeparam>
    /// <param name="httpMethod">The HTTP method to use (e.g., GET, POST).</param>
    /// <param name="endpoint">The API endpoint relative to the base URL.</param>
    /// <param name="jsonBody">An optional JSON payload to include in the request body, applicable for methods like POST or PUT.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the parsed API response.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API response indicates a failure.</exception>
    /// <exception cref="Exception">Thrown when an unexpected error occurs while sending the request.</exception>
    private async Task<BaseResponse<T>> SendRequestAsync<T>(HttpMethod httpMethod, string endpoint, string jsonBody = null)
    {
        using (var requestMessage = new HttpRequestMessage(httpMethod, _baseUrl + endpoint))
        {
            if (jsonBody != null)
            {
                requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            try
            {
                var responseMessage = await _httpClient.SendAsync(requestMessage);

                var response = await responseMessage.Content.ReadAsStringAsync();

                if (!responseMessage.IsSuccessStatusCode)
                {
                    var errorBuilder = new StringBuilder();
                    try
                    {
                        errorBuilder.Append(response.DeserializeKebabCase<ErrorResponse>().Error);
                    }
                    catch (Newtonsoft.Json.JsonSerializationException)
                    {
                        errorBuilder.Append(response);
                    }

                    throw new HttpRequestException(errorBuilder.ToString() + $",RequestUri: [{requestMessage.Method.Method}] {requestMessage.RequestUri}, Body: {jsonBody}", null, responseMessage.StatusCode);
                }

                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"{nameof(TastytradeApiClient)}:{nameof(SendRequestAsync)}.Response: {response}. RequestUri: {requestMessage.RequestUri}, Body: {jsonBody}");
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
