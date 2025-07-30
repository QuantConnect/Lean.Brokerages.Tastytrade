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
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace QuantConnect.Brokerages.Tastytrade.Models.Enum;

/// <summary>
/// Defines the types of events that can be sent from the server to the client over a stream or WebSocket connection.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum EventType
{
    /// <summary>
    /// The event type is unknown. This may indicate an unrecognized or malformed message.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A periodic keep-alive signal to ensure the connection remains active.
    /// Clients should respond or reset timeouts based on this message.
    /// </summary>
    [EnumMember(Value = "KEEPALIVE")]
    KeepAlive = 1,

    /// <summary>
    /// Indicates the beginning of a session setup sequence. 
    /// Clients may use this to initialize state or prepare for incoming data.
    /// </summary>
    [EnumMember(Value = "SETUP")]
    Setup = 2,

    /// <summary>
    /// A request from the server to the client to authenticate.
    /// This usually triggers the client to send credentials or a token.
    /// </summary>
    [EnumMember(Value = "AUTH")]
    Authorization = 3,

    /// <summary>
    /// Notification from the server regarding the client's current authorization state.
    /// The <c>state</c> field will be set to either <see cref="AuthorizationState.Authorized"/> or <see cref="AuthorizationState.Unauthorized"/>.
    /// </summary>
    [EnumMember(Value = "AUTH_STATE")]
    AuthorizationState = 4,

    /// <summary>
    /// A request to open a data or command channel.
    /// Clients may need to prepare handlers or acknowledge the request.
    /// </summary>
    [EnumMember(Value = "CHANNEL_REQUEST")]
    ChannelRequest = 5,

    /// <summary>
    /// Confirmation that a requested channel has been successfully opened.
    /// </summary>
    [EnumMember(Value = "CHANNEL_OPENED")]
    ChannelOpened = 6,

    /// <summary>
    /// Indicates an error occurred. The payload typically contains details about the error.
    /// Clients should log and/or display the error as appropriate.
    /// </summary>
    [EnumMember(Value = "ERROR")]
    Error = 7,

    /// <summary>
    /// An event to configure or initialize a data feed before use.
    /// This might contain metadata or schema definitions.
    /// </summary>
    [EnumMember(Value = "FEED_SETUP")]
    FeedSetup = 8,

    /// <summary>
    /// Configuration details or updates for a running data feed.
    /// Clients may need to adjust how they process incoming data.
    /// </summary>
    [EnumMember(Value = "FEED_CONFIG")]
    FeedConfig = 9,

    /// <summary>
    /// An event that signals subscription status to a data feed.
    /// This may include confirmation or failure details related to a subscription request.
    /// </summary>
    [EnumMember(Value = "FEED_SUBSCRIPTION")]
    FeedSubscription = 10,

    /// <summary>
    /// Real-time market data content sent by the feed after subscription.
    /// Typically includes quotes, trades, or other financial instruments data.
    /// </summary>
    [EnumMember(Value = "FEED_DATA")]
    FeedData = 11,

    /// <summary>
    /// An event related to orders placed, modified, or cancelled by the client or system.
    /// </summary>
    Order = 12,

    /// <summary>
    /// A notification containing account balance information, including updates to cash, margin, or other funds.
    /// </summary>
    AccountBalance = 13,

    /// <summary>
    /// </summary>
    CurrentPosition = 14,

    /// <summary>
    /// Indicates the current trading status of a symbol or market.
    /// </summary>
    TradingStatus = 15,

    /// <summary>
    /// A summary report of the year-to-date performance or gain of an underlying asset or portfolio.
    /// Useful for annual performance evaluation or reporting.
    /// </summary>
    UnderlyingYearGainSummary = 16,

    /// <summary>
    /// Represents a chain of related orders for a specific underlying symbol and account.
    /// Includes comprehensive metadata such as creation/update timestamps, 
    /// computed trade statistics (e.g., fees, gains, durations), order legs, and market state snapshots.
    /// Useful for reconstructing trading strategies, tracking order flows, and analyzing performance.
    /// </summary>
    OrderChain = 17
}
