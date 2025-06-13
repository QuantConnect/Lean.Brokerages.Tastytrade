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

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Represents the response data for a quote token request,
/// including connection details and access token information.
/// </summary>
public class ApiQuoteTokenResponse
{
    /// <summary>
    /// Gets the WebSocket URL for the dxLink connection.
    /// </summary>
    public string DxlinkUrl { get; set; }

    /// <summary>
    /// Gets the level of access (e.g., demo or live).
    /// </summary>
    public string Level { get; set; }

    /// <summary>
    /// Gets the authentication token used for accessing the quote service.
    /// </summary>
    /// <remarks>
    /// <b>Important:</b> API quote tokens expire after 24 hours.
    /// </remarks>
    public string Token { get; set; }
}