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

namespace QuantConnect.Brokerages.Tastytrade.Models.Enum;

/// <summary>
///Represents flags that indicate various states of an indexed event in a WebSocket message.
/// </summary>
/// <remarks>
/// The source <see href="https://github.com/dxFeed/dxfeed-graal-net-api/blob/2c16add3a5d95837d4caa24827d9001cd438bd46/src/DxFeed.Graal.Net/Events/EventFlags.cs#L12"/>
/// <seealso href="https://docs.dxfeed.com/dxfeed/api/com/dxfeed/event/IndexedEvent.html#getEventFlags--"/>
/// </remarks>
[Flags]
public enum EventFlag
{
    /// <summary>
    /// Indicates that no flags are set or the flag is unknown.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that an ongoing transactional update is in process.
    /// Events with this flag should be held in a pending list until the transaction completes.
    /// </summary>
    TxPending = 1 << 0, // bit 0

    /// <summary>
    /// Indicates that the event with the corresponding index should be removed.
    /// </summary>
    RemoveEvent = 1 << 1, // bit 1

    /// <summary>
    /// Indicates the start of loading a snapshot.
    /// On a new subscription, the first event for a non-zero source id may have this flag set,
    /// meaning a multi-event snapshot is incoming and events should be held until snapshot completion.
    /// </summary>
    SnapshotBegin = 1 << 2, // bit 2

    /// <summary>
    /// Indicates the end of a snapshot.
    /// This means the data source has sent all data pertaining to the subscription for this indexed event.
    /// </summary>
    SnapshotEnd = 1 << 3, // bit 3

    /// <summary>
    /// Indicates the snapshot was truncated due to reaching a data limit.
    /// More data might be available but will not be provided.
    /// This flag also marks the end of the snapshot, but differs from <see cref="SnapshotEnd"/> in that the snapshot is incomplete.
    /// </summary>
    SnapshotSnip = 1 << 4, // bit 4

    /// <summary>
    /// Used to activate snapshot mode without starting the snapshot synchronization protocol.
    /// This is intended for publishing only and switches on snapshot mode if not yet activated.
    /// </summary>
    SnapshotMode = 1 << 6 // bit 6
}
