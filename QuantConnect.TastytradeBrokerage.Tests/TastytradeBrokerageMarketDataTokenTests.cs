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
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Http;
using NUnit.Framework;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using QuantConnect.Configuration;
using System.Collections.Concurrent;
using QuantConnect.Brokerages.Tastytrade.Tests.Models;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

/// <summary>
/// The quote token the market data socket authenticates with. DxLink can revoke it before the 24 hours it is valid for,
/// and then answers every connection that sends it with an UNAUTHORIZED error.
/// </summary>
[TestFixture]
public class TastytradeBrokerageMarketDataTokenTests
{
    /// <summary>
    /// The error DxLink answers the AUTH step with once it revoked the token.
    /// </summary>
    private const string TokenExpiredError =
        """{"type":"ERROR","channel":0,"error":"UNAUTHORIZED","message":"Your authentication token has expired, reauthentication is required"}""";

    private const string AuthorizedState = """{"type":"AUTH_STATE","channel":0,"state":"AUTHORIZED","userId":"test-user"}""";

    private HttpListener _dxLinkServer;
    private string _dxLinkUrl;
    private int _connectionCount;

    /// <summary>
    /// How many connections, the first ones, DxLink answers with the UNAUTHORIZED error.
    /// </summary>
    private int _rejectedConnections;

    /// <summary>
    /// The token of every AUTH step DxLink received, in the order the connections sent them.
    /// </summary>
    private ConcurrentQueue<string> _authTokens;

    [SetUp]
    public void SetUp()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        _dxLinkUrl = $"ws://127.0.0.1:{port}/";
        _connectionCount = 0;
        _rejectedConnections = 1;
        _authTokens = new();
        _dxLinkServer = new HttpListener();
        _dxLinkServer.Prefixes.Add($"http://127.0.0.1:{port}/");
        _dxLinkServer.Start();
        Task.Run(AcceptDxLinkConnections);
    }

    [TearDown]
    public void TearDown()
    {
        _dxLinkServer.Stop();
        _dxLinkServer.Close();
    }

    [Test]
    public void ConnectAfterDxLinkRejectsTheQuoteTokenAuthenticatesWithANewToken()
    {
        var authTokens = ConnectToDxLink(out var quoteTokenRequests);

        Assert.That(authTokens, Is.EqualTo(new[] { "quote-token-1", "quote-token-2" }));
        Assert.That(quoteTokenRequests, Is.EqualTo(2));
    }

    [Test]
    public void ConnectAfterDxLinkRejectsANewQuoteTokenWithinAMinuteKeepsTheToken()
    {
        _rejectedConnections = 2;

        var authTokens = ConnectToDxLink(out var quoteTokenRequests);

        Assert.That(authTokens, Is.EqualTo(new[] { "quote-token-1", "quote-token-2", "quote-token-2" }));
        Assert.That(quoteTokenRequests, Is.EqualTo(2));
    }

    /// <summary>
    /// Connects a brokerage whose market data socket reaches the test DxLink server, and waits until DxLink authorized it.
    /// </summary>
    /// <param name="quoteTokenRequests">How many quote tokens the brokerage asked for.</param>
    /// <returns>The token of every AUTH step DxLink received, in the order the connections sent them.</returns>
    private string[] ConnectToDxLink(out int quoteTokenRequests)
    {
        var httpHandler = new QuoteTokenSequenceHttpHandler(_dxLinkUrl);
        var accountNumber = Config.Get("tastytrade-account-number");
        // Without an account number the brokerage opens only the market data socket, as a data queue handler does.
        Config.Set("tastytrade-account-number", "");
        try
        {
            using var brokerage = new TestableTastytradeBrokerage(httpHandler: httpHandler);
            brokerage.Connect();
            quoteTokenRequests = httpHandler.QuoteTokenRequests;
            return _authTokens.ToArray();
        }
        finally
        {
            Config.Set("tastytrade-account-number", accountNumber);
        }
    }

    /// <summary>
    /// Answers like DxLink after it revoked a token: the first <see cref="_rejectedConnections"/> connections get the UNAUTHORIZED error
    /// for their AUTH step, every later connection is authorized.
    /// </summary>
    private async Task AcceptDxLinkConnections()
    {
        while (_dxLinkServer.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _dxLinkServer.GetContextAsync();
            }
            catch (Exception)
            {
                return;
            }

            var connection = Interlocked.Increment(ref _connectionCount);
            _ = Task.Run(async () =>
            {
                try
                {
                    var webSocket = (await context.AcceptWebSocketAsync(null)).WebSocket;
                    var buffer = new byte[4096];
                    while (webSocket.State == WebSocketState.Open)
                    {
                        var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        if (!message.Contains("\"type\":\"AUTH\"", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        _authTokens.Enqueue(message.DeserializeCamelCase<Dictionary<string, string>>()["token"]);
                        var answer = connection <= _rejectedConnections ? TokenExpiredError : AuthorizedState;
                        await webSocket.SendAsync(Encoding.UTF8.GetBytes(answer), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                catch (Exception)
                {
                    // the client went away
                }
            });
        }
    }

    /// <summary>
    /// Hands out a new quote token, pointing at the test DxLink server, on every request.
    /// </summary>
    private class QuoteTokenSequenceHttpHandler : MockHttpMessageHandler
    {
        private readonly string _dxLinkUrl;
        private int _quoteTokenRequests;

        public int QuoteTokenRequests => _quoteTokenRequests;

        public QuoteTokenSequenceHttpHandler(string dxLinkUrl)
        {
            _dxLinkUrl = dxLinkUrl;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.RequestUri.AbsolutePath.EndsWith("/api-quote-tokens", StringComparison.Ordinal))
            {
                return base.Send(request, cancellationToken);
            }

            var token = $"quote-token-{Interlocked.Increment(ref _quoteTokenRequests)}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent($$"""{"data":{"dxlink-url":"{{_dxLinkUrl}}","level":"api","token":"{{token}}"},"context":"/api-quote-tokens"}""",
                    Encoding.UTF8, "application/json")
            };
        }
    }
}
