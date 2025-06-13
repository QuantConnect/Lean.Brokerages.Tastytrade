﻿/*
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

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Represents a generic response wrapper for API results, containing the response data and context information.
/// </summary>
/// <typeparam name="T">The type of the data contained in the response.</typeparam>
public class BaseResponse<T>
{
    /// <summary>
    /// Gets the data returned by the API.
    /// </summary>
    public T Data { get; set; }

    /// <summary>
    /// Gets additional context information about the API response.
    /// </summary>
    public string Context { get; set; }

    /// <summary>
    /// Gets pagination information for paged API responses, if applicable.
    /// </summary>
    public Pagination? Pagination { get; set; }
}

/// <summary>
/// Represents pagination details for API responses that return a subset of a larger dataset.
/// </summary>
public struct Pagination
{
    /// <summary>
    /// Gets or sets the number of items returned per page.
    /// </summary>
    public int PerPage { get; set; }

    /// <summary>
    /// Gets or sets the index offset of the current page within the total dataset.
    /// </summary>
    public int PageOffset { get; set; }

    /// <summary>
    /// Gets or sets the total number of items available across all pages.
    /// </summary>
    public int TotalPages { get; set; }
}