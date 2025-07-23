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
using System.Net.Http.Headers;
using QuantConnect.Brokerages.Authentication;
using QuantConnect.Brokerages.Tastytrade.Models;

namespace QuantConnect.Brokerages.Tastytrade.Api;

/// <summary>
/// Handles automatic session token management for HTTP requests, including creation, refreshing, and disposal of sessions.
/// </summary>
public sealed class SessionTokenHandler : TokenHandler
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
    /// The HTTP client used to send requests to the Tastytrade API.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionTokenHandler"/> class.
    /// </summary>
    /// <param name="baseUrl">The base URL for the Tastytrade API.</param>
    /// <param name="username">The username used for authentication.</param>
    /// <param name="password">The password used for authentication.</param>
    public SessionTokenHandler(string baseUrl, string username, string password) : base((tokenType, accessToken) => new AuthenticationHeaderValue(accessToken))
    {
        _username = username;
        _password = password;
        _baseUrlWithSessionEndpoint = baseUrl.TrimEnd('/') + "/sessions";
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Gets a valid session token, creating or refreshing as needed.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A valid session token string.</returns>
    public override (TokenType TokenType, string AccessToken) GetAccessToken(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_sessionToken))
        {
            _sessionToken = CreateSession(_username, _password, cancellationToken);
        }
        else if (DateTime.UtcNow >= _sessionExpirationTime)
        {
            _sessionToken = UpdateSession(_username, _rememberToken, cancellationToken);
        }

        return (TokenType.SessionToken, _sessionToken);
    }

    /// <summary>
    /// Creates a new session using username and password.
    /// </summary>
    /// <param name="username">Username for login.</param>
    /// <param name="password">Password for login.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new session token.</returns>
    private string CreateSession(string username, string password, CancellationToken cancellationToken)
    {
        return SendSession(new CreateSessionRequest(username, password.ToString()).ToJson(), cancellationToken);
    }

    /// <summary>
    /// Updates an existing session using a remember token.
    /// </summary>
    /// <param name="username">The username associated with the session.</param>
    /// <param name="rememberToken">The token used to refresh the session.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The refreshed session token.</returns>
    private string UpdateSession(string username, string rememberToken, CancellationToken cancellationToken)
    {
        return SendSession(new UpdateSessionRequest(username, rememberToken).ToJson(), cancellationToken);
    }

    /// <summary>
    /// Sends a session-related request to the server.
    /// </summary>
    /// <param name="jsonBody">The JSON payload of the session request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The session token received in the response.</returns>
    private string SendSession(string jsonBody, CancellationToken cancellationToken)
    {
        var response = ExecuteSessionRequest(HttpMethod.Post, jsonBody, cancellationToken);

        var jsonContent = response.ReadContentAsString(cancellationToken);

        var sessionResponse = jsonContent.DeserializeKebabCase<BaseResponse<SessionResponse>>().Data;

        _sessionExpirationTime = sessionResponse.SessionExpiration.AddSeconds(-10); // A 10-second buffer for expiration
        _rememberToken = sessionResponse.RememberToken;

        return sessionResponse.SessionToken;
    }

    /// <summary>
    /// Destroys the current session.
    /// </summary>
    private void DestroySessionAsync()
    {
        var response = ExecuteSessionRequest(HttpMethod.Delete, null, default);

        if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            _sessionToken = null;
            _rememberToken = null;
            Log.Trace($"{nameof(SessionTokenHandler)}.{nameof(DestroySessionAsync)}: Session destroyed successfully.");
        }
        else
        {
            var responseBody = response.ReadContentAsString();
            Log.Error($"{nameof(SessionTokenHandler)}.{nameof(DestroySessionAsync)}: Failed to destroy session. " +
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
    private HttpResponseMessage ExecuteSessionRequest(HttpMethod httpMethod, string jsonBody, CancellationToken cancellationToken)
    {
        using var requestMessage = new HttpRequestMessage(httpMethod, _baseUrlWithSessionEndpoint);

        if (!string.IsNullOrEmpty(jsonBody))
        {
            requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        var responseMessage = _httpClient.Send(requestMessage, cancellationToken);

        responseMessage.EnsureSuccessStatusCode(requestMessage, jsonBody);

        return responseMessage;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DestroySessionAsync();
        }
    }
}
