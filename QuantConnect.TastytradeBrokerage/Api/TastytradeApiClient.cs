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
using System.Web;
using System.Text;
using System.Net.Http;
using QuantConnect.Api;
using QuantConnect.Logging;
using System.Net.Http.Headers;
using System.Collections.Generic;
using QuantConnect.Brokerages.Authentication;
using QuantConnect.Brokerages.Tastytrade.Models;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Orders;

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
    /// Provides access tokens for authenticating API requests.
    /// </summary>
    public readonly TokenHandler TokenProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TastytradeApiClient"/> class using Lean token-based authentication.
    /// </summary>
    /// <param name="baseUrl">The base URL of the Tastytrade API.</param>
    /// <param name="brokerageName">The name of the brokerage associated with the token.</param>
    /// <param name="accountNumber">The account number linked to the request.</param>
    /// <param name="refreshToken">The refresh token used to obtain a new access token.</param>
    /// <param name="leanApiClient">The Lean API client instance.</param>
    public TastytradeApiClient(string baseUrl, string brokerageName, string accountNumber, string refreshToken, ApiConnection leanApiClient)
        : this(baseUrl, new OAuthTokenHandler<LeanAccessTokenMetaDataRequest, LeanAccessTokenMetaDataResponse>(leanApiClient, new(brokerageName, refreshToken, accountNumber)), accountNumber)
    {
    }

    /// <summary>
    /// <summary>
    /// Initializes a new instance of the <see cref="TastytradeApiClient"/> class using username and password authentication.
    /// </summary>
    /// <param name="baseUrl">The base URL of the Tastytrade API.</param>
    /// <param name="username">The Tastytrade account username.</param>
    /// <param name="password">The Tastytrade account password.</param>
    /// <param name="accountNumber">The account number linked to the request.</param>
    public TastytradeApiClient(string baseUrl, string username, string password, string accountNumber)
        : this(baseUrl, new SessionTokenHandler(baseUrl, username, password), accountNumber)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TastytradeApiClient"/> class using a custom token handler.
    /// </summary>
    /// <param name="baseUrl">The base URL of the Tastytrade API.</param>
    /// <param name="tokenHandler">The token handler used to authenticate API requests.</param>
    /// <param name="accountNumber">The account number associated with the Tastytrade account.</param>
    private TastytradeApiClient(string baseUrl, TokenHandler tokenHandler, string accountNumber)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient(tokenHandler);
        // All requests must include a User-Agent header or they will be rejected.
        /// <see href="https://developer.tastytrade.com/api-overview/#api-conventions-rest-json"/>
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("quantconnect-tastytrade-brokerage", "1.0"));
        TokenProvider = tokenHandler;
        AccountNumber = accountNumber;
    }

    /// <summary>
    /// Retrieves the account balances for the associated Tastytrade account.
    /// </summary>
    /// <returns>The result contains the account balances.</returns>
    public AccountBalance GetAccountBalances()
    {
        return SendRequest<AccountBalance>(HttpMethod.Get, $"/accounts/{AccountNumber}/balances", logResponse: true).Data;
    }

    /// <summary>
    /// Retrieves the current open positions for the associated Tastytrade account.
    /// </summary>
    /// <returns>The result contains a collection of positions.</returns>
    public IReadOnlyCollection<Position> GetAccountPositions()
    {
        return SendRequest<ResponseList<Position>>(HttpMethod.Get, $"/accounts/{AccountNumber}/positions", logResponse: true).Data.Items;
    }

    /// <summary>
    /// Retrieves the current API quote token from Tastytrade.
    /// </summary>
    /// <returns>The result contains the API quote token.</returns>
    public ApiQuoteTokenResponse GetApiQuoteToken()
    {
        return SendRequest<ApiQuoteTokenResponse>(HttpMethod.Get, "/api-quote-tokens").Data;
    }

    /// <summary>
    /// Retrieves an outright future instrument by its ticker symbol.
    /// </summary>
    /// <param name="futureTicker">The symbol of the future (e.g., "/ESM5").</param>
    /// <returns>
    /// The result contains a <see cref="Future"/> representing basic metadata of the requested future.
    /// </returns>
    public Future GetInstrumentFuture(string futureTicker)
    {
        return SendRequest<Future>(HttpMethod.Get, "/instruments/futures/" + futureTicker).Data;
    }

    /// <summary>
    /// Submits a brokerage order to the specified account.
    /// </summary>
    /// <param name="order">The brokerage order to be submitted.</param>
    /// <returns>
    /// The result contains the <see cref="OrderResponse"/> returned by the brokerage API.
    /// </returns>
    public OrderResponse SubmitOrder(OrderBaseRequest order)
    {
        return SendRequest<OrderResponse>(HttpMethod.Post, $"/accounts/{AccountNumber}/orders", order.ToJson()).Data;
    }

    /// <summary>
    /// Retrieves all active orders for the account with statuses: Received, Routed, InFlight, and Live.
    /// </summary>
    /// <returns>
    /// A read-only sequence of <see cref="Order"/> objects representing the current live orders.
    /// </returns>
    /// <remarks>
    /// Results are paginated and limited to 200 orders per page.
    /// </remarks>
    public IEnumerable<Order> GetLiveOrders()
    {
        foreach (var response in SendRequestWithPagination<ResponseList<Order>>($"/accounts/{AccountNumber}/orders?status[]={OrderStatus.Received}&status[]={OrderStatus.Routed}&status[]={OrderStatus.InFlight}&status[]={OrderStatus.Live}"))
        {
            foreach (var order in response.Data.Items)
            {
                yield return order;
            }
        }
    }

    /// <summary>
    /// Sends a cancellation request for the order associated with the specified unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the order to cancel.</param>
    /// <remarks>
    /// This method performs a synchronous DELETE request to the brokerage API to cancel the order.
    /// Use this when the order no longer needs to be executed or needs to be replaced.
    /// </remarks>
    public void CancelOrderById(string id)
    {
        SendRequest<object>(HttpMethod.Delete, $"/accounts/{AccountNumber}/orders/{id}");
    }

    /// <summary>
    /// Replaces an existing order by its ID with new order parameters.
    /// </summary>
    /// <param name="id">The unique identifier of the existing order to be replaced.</param>
    /// <param name="order">The new order request containing updated parameters.</param>
    /// <returns>
    /// The result contains the ID of the newly replaced order.
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown if the request to replace the order fails (e.g., due to network issues or an invalid response).
    /// </exception>
    public string ReplaceOrderById(string id, OrderBaseRequest order)
    {
        return SendRequest<Order>(HttpMethod.Patch, $"/accounts/{AccountNumber}/orders/{id}", order.ToJson()).Data.Id;
    }

    /// <summary>
    /// Determines whether the specified symbol corresponds to an index, based on its underlying equity definition.
    /// </summary>
    /// <param name="symbol">The equity option symbol to check.</param>
    /// <returns>
    /// The result contains <c>true</c> if the underlying equity is an index; otherwise, <c>false</c>.
    /// </returns>
    public bool IsUnderlyingEquityAnIndexAsync(string symbol)
    {
        return SendRequest<Equity>(HttpMethod.Get, $"/instruments/equities/{symbol.UrlEncodeSymbol()}").Data.IsIndex;
    }

    /// <summary>
    /// Retrieves the full list of future options for the given underlying future ticker symbol.
    /// </summary>
    /// <param name="underlyingFutureTicker">The ticker symbol of the underlying future.</param>
    /// <returns>
    /// A read-only collection of <see cref="FutureOption"/> objects representing the available options.
    /// </returns>
    public IReadOnlyCollection<FutureOption> GetFutureOptionChains(string underlyingFutureTicker)
    {
        return SendRequest<ResponseList<FutureOption>>(HttpMethod.Get, $"/futures-option-chains/{underlyingFutureTicker}").Data.Items;
    }

    /// <summary>
    /// Retrieves the list of equity instruments (option underlyings) available for a given ticker.
    /// </summary>
    /// <param name="ticker">The symbol of the underlying asset (e.g., <c>AAPL</c>, <c>SPX</c>).</param>
    /// <returns>
    ///The result contains a read-only collection of <see cref="Equity"/> instances associated with the specified ticker.
    /// </returns>
    public IReadOnlyCollection<Equity> GetOptionChains(string ticker)
    {
        return SendRequest<ResponseList<Equity>>(HttpMethod.Get, $"/option-chains/" + ticker).Data.Items;
    }

    /// <summary>
    /// Sends an HTTP request and parses the response from the Tastytrade API.
    /// </summary>
    /// <typeparam name="T">The type of the expected response data.</typeparam>
    /// <param name="httpMethod">The HTTP method to use (e.g., GET, POST).</param>
    /// <param name="endpoint">The API endpoint relative to the base URL.</param>
    /// <param name="jsonBody">An optional JSON payload to include in the request body, applicable for methods like POST or PUT.</param>
    /// <param name="logResponse">Indicates whether the response should be logged for debugging or auditing purposes.</param>
    /// <returns>The result contains the parsed API response.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API response indicates a failure.</exception>
    /// <exception cref="Exception">Thrown when an unexpected error occurs while sending the request.</exception>
    private BaseResponse<T> SendRequest<T>(HttpMethod httpMethod, string endpoint, string jsonBody = null, bool logResponse = default)
    {
        using (var requestMessage = new HttpRequestMessage(httpMethod, _baseUrl + endpoint))
        {
            if (jsonBody != null)
            {
                requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            try
            {
                var responseMessage = _httpClient.Send(requestMessage);

                responseMessage.EnsureSuccessStatusCode(requestMessage, jsonBody);

                var response = responseMessage.ReadContentAsString();

                if (logResponse || Log.DebuggingEnabled)
                {
                    Log.Trace($"{nameof(TastytradeApiClient)}:{nameof(SendRequest)}.Response: {response}. RequestUri: {requestMessage.RequestUri}, Body: {jsonBody}");
                }

                return response.DeserializeKebabCase<BaseResponse<T>>();
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(TastytradeApiClient)}.{nameof(SendRequest)}: Unexpected error while sending request - {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Sends paginated GET requests to the specified endpoint and yields each page of results.
    /// </summary>
    /// <typeparam name="T">The type of the data returned in the response.</typeparam>
    /// <param name="endpoint">The base URI endpoint to send the requests to. Pagination parameters will be appended.</param>
    /// <returns>
    /// An <see cref="IEnumerable{BaseResponse}"/> of responses containing data of type <typeparamref name="T"/>.
    /// </returns>
    private IEnumerable<BaseResponse<T>> SendRequestWithPagination<T>(string endpoint)
    {
        var currentPageOffset = default(int);

        if (!endpoint.EndsWith('&'))
        {
            endpoint += "&";
        }

        var response = default(BaseResponse<T>);

        var parsedQuery = HttpUtility.ParseQueryString("per-page=200");
        do
        {
            parsedQuery["page-offset"] = currentPageOffset.ToString();

            response = SendRequest<T>(HttpMethod.Get, endpoint + parsedQuery.ToString());

            yield return response;

        } while (++currentPageOffset < response.Pagination?.TotalPages);
    }
}
