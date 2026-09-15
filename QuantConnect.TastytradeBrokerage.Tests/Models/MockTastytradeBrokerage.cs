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

namespace QuantConnect.Brokerages.Tastytrade.Tests.Models;

/// <summary>
/// Brokerage without any connection, so a test can report socket status changes directly.
/// </summary>
public class MockTastytradeBrokerage : TastytradeBrokerage
{
    /// <summary>
    /// Reports a socket status change the way the socket wrappers and the stream handlers do.
    /// </summary>
    /// <param name="sender">The socket that reports it.</param>
    /// <param name="messageType"><see cref="BrokerageMessageType.Disconnect"/> or <see cref="BrokerageMessageType.Reconnect"/>.</param>
    /// <param name="reason">What happened to the socket.</param>
    public void ReportConnectionStatus(object sender, BrokerageMessageType messageType, string reason)
    {
        OnConnectionStatusChanged(sender, messageType, reason);
    }
}
