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

using Moq;
using System;
using NUnit.Framework;
using QuantConnect.Util;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture]
public class TastytradeBrokerageFactoryTests
{
    [TestCase("", "", "", "", null, true, "Missing key: tastytrade-account-number")]
    public void InitializesFactoryFromComposer(string baseUrl, string baseWSUrl, string userName, string password, string accountNumber, bool shouldThrowException, string exceptionMessage)
    {
        using var factory = Composer.Instance.Single<IBrokerageFactory>(instance => instance.BrokerageType == typeof(TastytradeBrokerage));
        Assert.IsNotNull(factory);

        var newBrokerageData = new Dictionary<string, string>();

        if (baseUrl != null)
        {
            newBrokerageData["tastytrade-api-url"] = baseUrl;
        }

        if (baseWSUrl != null)
        {
            newBrokerageData["tastytrade-websocket-url"] = baseWSUrl;
        }

        if (userName != null)
        {
            newBrokerageData["tastytrade-username"] = userName;
        }

        if (password != null)
        {
            newBrokerageData["tastytrade-password"] = password;
        }

        if (accountNumber != null)
        {
            newBrokerageData["tastytrade-account-number"] = accountNumber;
        }

        var liveNodePacket = new LiveNodePacket() { BrokerageData = newBrokerageData };

        var exception = Assert.Throws<ArgumentException>(() => factory.CreateBrokerage(liveNodePacket, new Mock<IAlgorithm>().Object));
        Assert.True(exception.Message.EndsWith(exceptionMessage));
    }
}