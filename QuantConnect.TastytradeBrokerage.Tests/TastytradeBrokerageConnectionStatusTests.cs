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

using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Tests.Models;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

/// <summary>
/// The Disconnect and Reconnect messages of a brokerage with two sockets: one Disconnect when the first socket drops,
/// one Reconnect once the last dropped socket is usable again.
/// </summary>
[TestFixture]
public class TastytradeBrokerageConnectionStatusTests
{
    [Test]
    public void OnConnectionStatusChangedWhenBothSocketsDropReportsDisconnectOnceAndReconnectWhenBothAreBack()
    {
        // Arrange
        using var brokerage = TestableTastytradeBrokerage.CreateWithoutConnection();
        var reportedMessages = new List<BrokerageMessageEvent>();
        brokerage.Message += (_, message) => reportedMessages.Add(message);
        var accountSocket = new object();
        var marketDataSocket = new object();

        // Act
        brokerage.ReportConnectionStatus(accountSocket, BrokerageMessageType.Disconnect, "account socket lost");
        brokerage.ReportConnectionStatus(marketDataSocket, BrokerageMessageType.Disconnect, "market data socket lost");
        brokerage.ReportConnectionStatus(accountSocket, BrokerageMessageType.Reconnect, "account socket restored");
        var reportedWhileMarketDataIsDown = reportedMessages.Select(message => message.Type).ToList();
        brokerage.ReportConnectionStatus(marketDataSocket, BrokerageMessageType.Reconnect, "market data socket restored");

        // Assert
        Assert.That(reportedWhileMarketDataIsDown, Is.EqualTo(new[] { BrokerageMessageType.Disconnect }));
        Assert.That(reportedMessages.Select(message => message.Type), Is.EqualTo(new[] { BrokerageMessageType.Disconnect, BrokerageMessageType.Reconnect }));
        Assert.That(reportedMessages[0].Message, Is.EqualTo("account socket lost"));
        Assert.That(reportedMessages[1].Message, Is.EqualTo("market data socket restored"));
    }

    [Test]
    public void OnConnectionStatusChangedWhenSocketConnectsForTheFirstTimeReportsNothing()
    {
        // Arrange
        using var brokerage = TestableTastytradeBrokerage.CreateWithoutConnection();
        var reportedMessages = new List<BrokerageMessageEvent>();
        brokerage.Message += (_, message) => reportedMessages.Add(message);

        // Act
        brokerage.ReportConnectionStatus(new object(), BrokerageMessageType.Reconnect, "market data socket ready");

        // Assert
        Assert.That(reportedMessages, Is.Empty);
    }
}
