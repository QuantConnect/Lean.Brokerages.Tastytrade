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

using System;
using Newtonsoft.Json;
using QuantConnect.Api;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Represents the response containing access token metadata from Lean authentication.
/// </summary>
public class LeanAccessTokenMetaDataResponse : RestResponse
{
    /// <summary>
    /// Gets the access token provided by Lean.
    /// </summary>
    public string AccessToken { get; }

    /// <summary>
    /// Gets the type of the token (e.g., "Bearer").
    /// </summary>
    public TokenType TokenType { get; }

    /// <summary>
    /// Gets the UTC expiration timestamp of the access token, with a 1-minute safety buffer applied.
    /// </summary>
    public DateTime AccessTokenExpires { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LeanAccessTokenMetaDataResponse"/> class.
    /// </summary>
    /// <param name="accessToken">The access token returned by Lean.</param>
    /// <param name="tokenType">The type of token returned (e.g., "Bearer").</param>
    /// <param name="expiresIn">The token lifetime in seconds, provided by Lean.</param>
    [JsonConstructor]
    public LeanAccessTokenMetaDataResponse(string accessToken, TokenType tokenType, int expiresIn)
    {
        AccessToken = accessToken;
        TokenType = tokenType;
        AccessTokenExpires = DateTime.UtcNow.AddSeconds(expiresIn).AddMinutes(-1); // 1-minute buffer to account for clock drift
    }
}
