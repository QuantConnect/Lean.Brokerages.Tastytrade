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
using System.Threading;

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Provides a reference-counted synchronization lock based on <see cref="SemaphoreSlim"/>.
/// Ensures proper disposal when no more threads are holding the lock.
/// </summary>
public class RefCountedLock : IDisposable
{
    /// <summary>
    /// Internal semaphore used to enforce mutual exclusion.
    /// </summary>
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Tracks the number of active threads holding or waiting for the semaphore.
    /// </summary>
    private int _activeCount = 0;

    /// <summary>
    /// Enters the lock, blocking the calling thread until the lock becomes available.
    /// Increments the internal active reference count.
    /// </summary>
    public void Enter()
    {
        Interlocked.Increment(ref _activeCount);
        _semaphore.Wait();
    }

    /// <summary>
    /// Exits the lock and decrements the internal reference count.
    /// If no more active users remain, the semaphore is disposed.
    /// </summary>
    /// <returns>True if the semaphore was disposed; otherwise, false.</returns>
    public bool Exit()
    {
        _semaphore.Release();

        if (Interlocked.Decrement(ref _activeCount) == 0)
        {
            Dispose();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Disposes the underlying semaphore if not already disposed.
    /// Safe to call multiple times.
    /// </summary>
    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
