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
using QuantConnect.Brokerages.Tastytrade.Serialization;

namespace QuantConnect.Brokerages.Tastytrade.Models;

/// <summary>
/// Represents a request to create a new session using login credentials.
/// </summary>
public sealed class CreateSessionRequest
{
    /// <summary>
    /// Gets the login identifier (e.g., username or email).
    /// </summary>
    public string Login { get; }

    /// <summary>
    /// Gets the password associated with the login.
    /// </summary>
    public string Password { get; }

    /// <summary>
    /// Gets a value indicating whether the session should be remembered.
    /// </summary>
    public bool RememberMe { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSessionRequest"/> class.
    /// </summary>
    /// <param name="login">The login identifier.</param>
    /// <param name="password">The password for the login.</param>
    /// <param name="rememberMe">Indicates whether to remember the session. Defaults to <c>true</c>.</param>
    public CreateSessionRequest(string login, string password, bool rememberMe = true)
    {
        Login = login;
        Password = password;
        RememberMe = rememberMe;
    }

    /// <summary>
    /// Serializes the <see cref="UpdateSessionRequest"/> instance to a JSON string.
    /// </summary>
    /// <returns>A JSON string representation of the object.</returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, JsonSettings.KebabCase);
    }
}
