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
using System.Timers;
using QuantConnect.Brokerages.Tastytrade.Api;

namespace QuantConnect.Brokerages.Tastytrade.WebSocket;

public abstract class BaseWebSocketClientWrapper : WebSocketClientWrapper
{
    /// <summary>
    /// The timer responsible for triggering keep-alive messages at regular intervals.
    /// </summary>
    private Timer _timer;

    /// <summary>
    /// The interval, in seconds, at which keep-alive messages are sent to maintain the WebSocket connection.
    /// </summary>
    private readonly int _keepAliveIntervalSeconds;

    /// <summary>
    /// Provides methods for interacting with the Tastytrade API.
    /// </summary>
    protected readonly TastytradeApiClient _tastyTradeApiClient;

    /// <summary>
    /// A synchronization primitive used to signal that authentication has completed successfully.
    /// Child classes set this event once authentication is confirmed, allowing dependent workflows to proceed.
    /// </summary>
    public readonly System.Threading.AutoResetEvent AuthenticatedResetEvent = new(false);

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseWebSocketClientWrapper"/> class,
    /// configuring the API client and the keep-alive timer interval.
    /// </summary>
    /// <param name="tastytradeApiClient">The Tastytrade API client used for authentication and data requests.</param>
    /// <param name="keepAliveIntervalSeconds">The interval in seconds at which keep-alive messages will be sent. Default is 20 seconds.</param>
    protected BaseWebSocketClientWrapper(TastytradeApiClient tastytradeApiClient, int keepAliveIntervalSeconds = 20)
    {
        _tastyTradeApiClient = tastytradeApiClient;
        _keepAliveIntervalSeconds = keepAliveIntervalSeconds;
    }

    /// <summary>
    /// Event invocator for the <see cref="WebSocketClientWrapper.Open"/> event
    /// </summary>
    protected override void OnOpen()
    {
        base.OnOpen();

        CleanUpTimer();
        _timer = new Timer(TimeSpan.FromSeconds(_keepAliveIntervalSeconds).TotalMilliseconds);
        _timer.Elapsed += SendMessageByTimerElapsed;
        _timer.Start();
    }

    /// <summary>
    /// Event invocator for the <see cref="WebSocketClientWrapper.Close"/> event
    /// </summary>
    protected override void OnClose(WebSocketCloseData e)
    {
        CleanUpTimer();
        base.OnClose(e);
    }

    /// <summary>
    /// Event invocator for the <see cref="WebSocketClientWrapper.OnError"/> event
    /// </summary>
    protected override void OnError(WebSocketError e)
    {
        CleanUpTimer();
        base.OnError(e);
    }

    /// <summary>
    /// Helper method to clean up timer if required
    /// </summary>
    private void CleanUpTimer()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Elapsed -= SendMessageByTimerElapsed;
            _timer.Dispose();
            _timer = null;
        }
    }

    protected abstract void SendMessageByTimerElapsed(object sender, ElapsedEventArgs e);
}
