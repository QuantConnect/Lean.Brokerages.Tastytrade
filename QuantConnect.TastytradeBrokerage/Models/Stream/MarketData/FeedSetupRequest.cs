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

using Newtonsoft.Json;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Serialization;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

/// <summary>
/// Represents a FEED_SETUP message sent to the FEED service after opening a channel.
/// This message configures which data fields to receive on the specified channel.
/// </summary>
public readonly struct FeedSetupRequest
{
    /// <summary>
    /// Gets the constant event type for the setup message.
    /// This value is always "FEED_SETUP".
    /// </summary>
    public EventType Type => EventType.FeedSetup;

    /// <summary>
    /// Gets the channel identifier. Clients must set this to the channel they opened.
    /// </summary>
    public int Channel => 1;

    /// <summary>
    /// Gets the desired data format for FEED_DATA messages.
    /// Allowed values are "FULL" and "COMPACT".
    /// </summary>
    public string AcceptDataFormat => "FULL";

    /// <summary>
    /// Gets the accepted fields for each supported event type (Quote, Trade, etc.).
    /// This tells the server which specific fields should be included in data messages.
    /// </summary>
    public AcceptEventFields AcceptEventFields => new();

    /// <summary>
    /// Serializes the FeedSetup configuration into a JSON string using camelCase naming.
    /// </summary>
    /// <returns>A JSON string representation of the feed setup configuration.</returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, JsonSettings.CamelCase);
    }
}

/// <summary>
/// Represents the accepted data fields for each event type in the FEED_SETUP message.
/// </summary>
public readonly struct AcceptEventFields
{
    /// <summary>
    /// Gets the list of fields to receive for Quote events.
    /// </summary>
    /// <remarks>NOTE: The following Quote fields — "bidTime", "askTime", "timeNanoPart", "eventTime" — are known to return 0 from the API.</remarks>
    [JsonProperty(nameof(Quote))]
    public string[] Quote => ["eventSymbol", "bidPrice", "askPrice", "bidSize", "askSize"];

    /// <summary>
    /// Gets the list of fields to receive for Trade events.
    /// </summary>
    [JsonProperty(nameof(Trade))]
    public string[] Trade => ["eventSymbol", "price", "size", "time"];

    /// <summary>
    /// Gets the list of fields to receive for Summary events.
    /// </summary>
    [JsonProperty(nameof(Summary))]
    public string[] Summary => ["eventSymbol", "openInterest"];
}
