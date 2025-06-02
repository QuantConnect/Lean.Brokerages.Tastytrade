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
using QuantConnect.Util;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Configuration;

namespace QuantConnect.Brokerages.Tastytrade;

/// <summary>
/// Provides a Tastytrade implementation of BrokerageFactory
/// </summary>
public class TastytradeBrokerageFactory : BrokerageFactory
{
    /// <summary>
    /// Gets the brokerage data required to run the brokerage from configuration/disk
    /// </summary>
    /// <remarks>
    /// The implementation of this property will create the brokerage data dictionary required for
    /// running live jobs. See <see cref="IJobQueueHandler.NextJob"/>
    /// </remarks>
    public override Dictionary<string, string> BrokerageData => new()
    {
        {"tastytrade-api-url", Config.Get("tastytrade-api-url") },
        {"tastytrade-websocket-url", Config.Get("tastytrade-websocket-url")},
        {"tastytrade-username", Config.Get("tastytrade-username")},
        {"tastytrade-password",  Config.Get("tastytrade-password")},
        {"tastytrade-account-number",  Config.Get("tastytrade-account-number")},
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="TastytradeBrokerageFactory"/> class
    /// </summary>
    public TastytradeBrokerageFactory() : base(typeof(TastytradeBrokerage))
    {
    }

    /// <summary>
    /// Gets a brokerage model that can be used to model this brokerage's unique behaviors
    /// </summary>
    /// <param name="orderProvider">The order provider</param>
    public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider) => new TastytradeBrokerageModel();

    /// <summary>
    /// Creates a new IBrokerage instance
    /// </summary>
    /// <param name="job">The job packet to create the brokerage for</param>
    /// <param name="algorithm">The algorithm instance</param>
    /// <returns>A new brokerage instance</returns>
    public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
    {
        var errors = new List<string>();

        var baseUrl = Read<string>(job.BrokerageData, "tastytrade-api-url", errors);
        var baseWSUrl = Read<string>(job.BrokerageData, "tastytrade-websocket-url", errors);
        var userName = Read<string>(job.BrokerageData, "tastytrade-username", errors);
        var password = Read<string>(job.BrokerageData, "tastytrade-password", errors);
        var accountNumber = Read<string>(job.BrokerageData, "tastytrade-account-number", errors);

        if (errors.Count != 0)
        {
            // if we had errors then we can't create the instance
            throw new ArgumentException(string.Join(Environment.NewLine, errors));
        }

        var tt = new TastytradeBrokerage(baseUrl, baseWSUrl, userName, password, accountNumber, algorithm);

        Composer.Instance.AddPart<IDataQueueHandler>(tt);

        return tt;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public override void Dispose()
    {
        // Not needed
    }
}