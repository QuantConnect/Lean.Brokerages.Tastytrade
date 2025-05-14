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
using NUnit.Framework;
using QuantConnect.Brokerages.Tastytrade.Models;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture]
public class TastytradeJsonConverterTests
{
    [Test]
    public void ReturnsCorrectJsonRepresentationForValidCreateSession()
    {
        var createSession = new CreateSession("ramses", "pharaoh");

        var createSessionJson = createSession.ToJson();

        Assert.IsNotNull(createSessionJson);
        Assert.AreEqual("{\"login\":\"ramses\",\"password\":\"pharaoh\",\"remember-me\":true}", createSessionJson);
    }

    [Test]
    public void DeserializeErrorMessage()
    {
        var json = @"{""error"":{""code"":""not_permitted"",""message"":""User not permitted access""}}";

        var error = json.DeserializeKebabCase<ErrorResponse>().Error;

        Assert.IsNotNull(error.Code);
        Assert.IsNotEmpty(error.Code);
        Assert.IsNotNull(error.Message);
        Assert.IsNotEmpty(error.Message);
    }


    [Test]
    public void DeserializeCreateSessionResponseFromJsonFile()
    {
        var jsonContent = System.IO.File.ReadAllText("TestData\\Create_Session_Response.json");

        var sessionResponse = jsonContent.DeserializeKebabCase<BaseResponse<SessionResponse>>();

        Assert.IsNotNull(sessionResponse);
        Assert.IsNotNull(sessionResponse.Data);
        AssertIsNotNullAndIsNotEmpty(sessionResponse.Data.RememberToken, sessionResponse.Data.SessionToken, sessionResponse.Context);
        Assert.AreEqual("/sessions", sessionResponse.Context);
        Assert.AreNotEqual(default(DateTime), sessionResponse.Data.SessionExpiration);
    }

    [Test]
    public void DeserializeGetPositionsResponseFromJsonFile()
    {
        var jsonContent = System.IO.File.ReadAllText("TestData\\Get_Positions.json");

        var positions = jsonContent.DeserializeKebabCase<BaseResponse<ResponseList<Position>>>().Data.Items;

        Assert.IsNotNull(positions);
    }

    [Test]
    public void DeserializeGetApiQuoteToken()
    {
        var jsonContent = System.IO.File.ReadAllText("TestData\\Get_Api_Quote_Token.json");

        var apiQuoteTokenResponse = jsonContent.DeserializeKebabCase<BaseResponse<ApiQuoteTokenResponse>>();

        Assert.IsNotNull(apiQuoteTokenResponse);
        Assert.IsNotNull(apiQuoteTokenResponse.Data);
        AssertIsNotNullAndIsNotEmpty(apiQuoteTokenResponse.Data.DxlinkUrl, apiQuoteTokenResponse.Data.Level, apiQuoteTokenResponse.Data.Token);
        Assert.AreEqual("/api-quote-tokens", apiQuoteTokenResponse.Context);
    }

    private static void AssertIsNotNullAndIsNotEmpty(params string[] expected)
    {
        foreach (var item in expected)
        {
            Assert.IsNotNull(item);
            Assert.IsNotEmpty(item);
        }
    }
}