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

namespace QuantConnect.Brokerages.Tastytrade.Models.Enum;

/// <summary>
/// Represents event flags in a WebSocket message.
/// </summary>
/// <remarks>
/// The source <see href="https://github.com/dxFeed/dxfeed-graal-net-api/blob/2c16add3a5d95837d4caa24827d9001cd438bd46/src/DxFeed.Graal.Net/Events/EventFlags.cs#L12"/>
/// </remarks>
public enum EventFlag
{
    /// <summary>
    /// Indicates that the event flag is unknown or not set.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Indicates when the loading of a snapshot starts.
    /// </summary>
    SnapshotBegin = 4,

    /// <summary>
    /// <see cref="SnapshotEnd"/> or <see cref="SnapshotSnip"/> indicates the end of a snapshot.
    /// The difference between <see cref="SnapshotEnd"/>  and <see cref="SnapshotSnip"/> is the following:
    /// <see cref="SnapshotEnd"/> indicates that the data source sent all the data pertaining to
    /// the subscription for the corresponding indexed event,
    /// while <see cref="SnapshotSnip"/> indicates that some limit on the amount of data was reached
    /// and while there still might be more data available, it will not be provided.
    /// </summary>
    SnapshotEnd = 8,

    /// <summary>
    /// <see cref="SnapshotEnd"/> or <see cref="SnapshotSnip"/> indicates the end of a snapshot.
    /// The difference between <see cref="SnapshotEnd"/>  and <see cref="SnapshotSnip"/> is the following:
    /// <see cref="SnapshotEnd"/> indicates that the data source sent all the data pertaining to
    /// the subscription for the corresponding indexed event,
    /// while <see cref="SnapshotSnip"/> indicates that some limit on the amount of data was reached
    /// and while there still might be more data available, it will not be provided.
    /// </summary>
    SnapshotSnip = 10
}
