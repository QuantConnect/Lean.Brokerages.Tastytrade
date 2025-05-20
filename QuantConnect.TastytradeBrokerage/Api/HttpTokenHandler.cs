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
using System.Net.Http;
using System.Threading;
using QuantConnect.Logging;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using QuantConnect.Brokerages.Tastytrade.Models;

namespace QuantConnect.Brokerages.Tastytrade.Api;

/// <summary>
/// Handles automatic session token management for HTTP requests, including creation, refreshing, and disposal of sessions.
/// </summary>
public sealed class HttpTokenHandler : DelegatingHandler
{
    /// <summary>
    /// Full URL used for session-related API requests (e.g., "https://tasty.com/sessions").
    /// </summary>
    private readonly string _baseUrlWithSessionEndpoint;

    /// <summary>
    /// The username used for authentication.
    /// </summary>
    private readonly string _username;

    /// <summary>
    /// The password used for authentication.
    /// </summary>
    private readonly string _password;

    /// <summary>
    /// The remember token used to refresh the session.
    /// </summary>
    private string _rememberToken;

    /// <summary>
    /// The current session token used for authorization.
    /// </summary>
    private string _sessionToken;

    /// <summary>
    /// The UTC time when the current session expires.
    /// </summary>
    private DateTime _sessionExpirationTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpTokenHandler"/> class.
    /// </summary>
    /// <param name="baseUrl">The base URL for the Tastytrade API.</param>
    /// <param name="username">The username used for authentication.</param>
    /// <param name="password">The password used for authentication.</param>
    public HttpTokenHandler(string baseUrl, string username, string password) : base(new HttpClientHandler())
    {
        _username = username;
        _password = password;
        _baseUrlWithSessionEndpoint = baseUrl.TrimEnd('/') + "/sessions";
    }

    /// <inheritdoc/>
    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _sessionToken = await GetSessionToken(cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue(_sessionToken);

        return await base.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Gets a valid session token, creating or refreshing as needed.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A valid session token string.</returns>
    private async Task<string> GetSessionToken(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_sessionToken))
        {
            _sessionToken = await CreateSession(_username, _password, cancellationToken);
        }
        else if (DateTime.UtcNow >= _sessionExpirationTime)
        {
            _sessionToken = await UpdateSession(_username, _rememberToken, cancellationToken);
        }

        return _sessionToken;
    }

    /// <summary>
    /// Creates a new session using username and password.
    /// </summary>
    /// <param name="username">Username for login.</param>
    /// <param name="password">Password for login.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new session token.</returns>
    private async Task<string> CreateSession(string username, string password, CancellationToken cancellationToken)
    {
        return await SendSessionAsync(new CreateSession(username, password.ToString()).ToJson(), cancellationToken);
    }

    /// <summary>
    /// Updates an existing session using a remember token.
    /// </summary>
    /// <param name="username">The username associated with the session.</param>
    /// <param name="rememberToken">The token used to refresh the session.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The refreshed session token.</returns>
    private async Task<string> UpdateSession(string username, string rememberToken, CancellationToken cancellationToken)
    {
        return await SendSessionAsync(new UpdateSession(username, rememberToken).ToJson(), cancellationToken);
    }

    /// <summary>
    /// Sends a session-related request to the server.
    /// </summary>
    /// <param name="jsonBody">The JSON payload of the session request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The session token received in the response.</returns>
    private async Task<string> SendSessionAsync(string jsonBody, CancellationToken cancellationToken)
    {
        var response = await ExecuteSessionRequestAsync(HttpMethod.Post, jsonBody, cancellationToken);

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);

        var sessionResponse = jsonContent.DeserializeKebabCase<BaseResponse<SessionResponse>>().Data;

        _sessionExpirationTime = sessionResponse.SessionExpiration.AddSeconds(-10); // A 10-second buffer for expiration
        _rememberToken = sessionResponse.RememberToken;

        return sessionResponse.SessionToken;
    }

    /// <summary>
    /// Destroys the current session.
    /// </summary>
    private async Task DestroySessionAsync()
    {
        var response = await ExecuteSessionRequestAsync(HttpMethod.Delete, null, default);

        if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            _sessionToken = null;
            _rememberToken = null;
            Log.Trace($"{nameof(HttpTokenHandler)}.{nameof(DestroySessionAsync)}: Session destroyed successfully.");
        }
        else
        {
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Log.Error($"{nameof(HttpTokenHandler)}.{nameof(DestroySessionAsync)}: Failed to destroy session. " +
                     $"StatusCode: {response.StatusCode}, Response: {responseBody}");
        }
    }

    /// <summary>
    /// Executes a session-related HTTP request.
    /// </summary>
    /// <param name="httpMethod">The HTTP method (POST/DELETE).</param>
    /// <param name="jsonBody">The JSON payload, if any.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    private async Task<HttpResponseMessage> ExecuteSessionRequestAsync(HttpMethod httpMethod, string jsonBody, CancellationToken cancellationToken)
    {
        using var requestMessage = new HttpRequestMessage(httpMethod, _baseUrlWithSessionEndpoint);

        if (!string.IsNullOrEmpty(jsonBody))
        {
            requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        var responseMessage = await base.SendAsync(requestMessage, cancellationToken);

        if (!responseMessage.IsSuccessStatusCode)
        {
            var jsonContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
            var error = jsonContent.DeserializeKebabCase<ErrorResponse>().Error;
            throw new HttpRequestException(error.ToString(), null, responseMessage.StatusCode);
        }

        return responseMessage;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DestroySessionAsync().SynchronouslyAwaitTask();
        }
    }
}
