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
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;
using QuantConnect.Brokerages.Authentication;

namespace QuantConnect.Brokerages.Tastytrade.Tests.Models;

/// <summary>
/// Answers the REST requests of the Tastytrade API client with answers captured from the real API,
/// so a test runs the real brokerage code without any connection.
/// </summary>
public class MockHttpMessageHandler : LeanTokenHandler<LeanTokenCredentials>
{
    /// <summary>
    /// The captured answers, each one for a request method and the end of the request path.
    /// </summary>
    private readonly List<(HttpMethod Method, string PathEnding, string Json)> _responses = [];

    /// <summary>
    /// Registers the quote token answer every brokerage needs: the market data socket asks for it as soon as the brokerage is created.
    /// </summary>
    public MockHttpMessageHandler()
    {
        SetResponse(HttpMethod.Get, "/api-quote-tokens", File.ReadAllText(Path.Combine("TestData", "Get_Api_Quote_Token.json")));
    }

    /// <summary>
    /// Registers the answer for a request. The path ending leaves the account number out,
    /// for example <c>/orders</c> or <c>/orders/507440771</c>.
    /// </summary>
    /// <param name="method">The method of the request.</param>
    /// <param name="pathEnding">The end of the request path, without the query.</param>
    /// <param name="json">The JSON body captured from the real API.</param>
    public void SetResponse(HttpMethod method, string pathEnding, string json)
    {
        _responses.Add((method, pathEnding, json));
    }

    /// <summary>
    /// Returns a token that is never sent anywhere.
    /// </summary>
    /// <param name="cancellationToken">This parameter is not used.</param>
    public override LeanTokenCredentials GetAccessToken(CancellationToken cancellationToken)
    {
        return new LeanTokenCredentials(TokenType.Bearer, "fake-access-token");
    }

    /// <summary>
    /// Returns the registered answer instead of sending the request. A request nobody registered gets a 404
    /// that names it, so the test fails with the missing request in the error.
    /// </summary>
    /// <param name="request">The request of the API client.</param>
    /// <param name="cancellationToken">This parameter is not used.</param>
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        foreach (var response in _responses)
        {
            if (response.Method == request.Method && request.RequestUri.AbsolutePath.EndsWith(response.PathEnding, StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                    Content = new StringContent(response.Json, Encoding.UTF8, "application/json")
                };
            }
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            RequestMessage = request,
            Content = new StringContent($"No captured answer was registered for {request.Method} {request.RequestUri}")
        };
    }
}
