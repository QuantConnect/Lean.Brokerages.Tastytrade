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
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Models.Stream;

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

    [Test]
    public void SerializeStreamHeartbeatMessage()
    {
        var heartbeatJson = new Heartbeat("your session token here", 1).ToJson();

        Assert.AreEqual("{\"action\":\"heartbeat\",\"auth-token\":\"your session token here\",\"request-id\":1}", heartbeatJson);
    }

    [Test]
    public void DeserializeStreamHeartbeatMessage()
    {
        var heartbeatResponseJson = "{\"status\":\"ok\",\"action\":\"heartbeat\",\"web-socket-session-id\":\"13ec76b6\",\"request-id\":3}";

        var connectResponse = heartbeatResponseJson.DeserializeKebabCase<HeartbeatResponse>();

        Assert.AreEqual(Status.Ok, connectResponse.Status);
        Assert.AreEqual(ActionStream.Heartbeat, connectResponse.Action);
        Assert.AreEqual(3, connectResponse.RequestId);
        AssertIsNotNullAndIsNotEmpty(connectResponse.WebSocketSessionId);
    }

    [Test]
    public void SerializeStreamConnectMessage()
    {
        var connectJson = new Connect("your session token here", 1, "12345").ToJson();

        Assert.AreEqual("{\"action\":\"connect\",\"value\":[\"12345\"],\"auth-token\":\"your session token here\",\"request-id\":1}", connectJson);
    }

    [Test]
    public void DeserializeConnectResponseStatusOk()
    {
        var connectResponseJson = "{\"status\":\"ok\",\"action\":\"connect\",\"web-socket-session-id\":\"c8531fa0\",\"value\":[\"5WX06827\"],\"request-id\":1}";

        var connectResponse = connectResponseJson.DeserializeKebabCase<ConnectResponse>();

        Assert.IsInstanceOf<BaseResponseMessage>(connectResponse);
        Assert.AreEqual(Status.Ok, connectResponse.Status);
        Assert.AreEqual(ActionStream.Connect, connectResponse.Action);
        Assert.AreEqual(1, connectResponse.RequestId);
        Assert.Greater(connectResponse.AccountNumbers.Length, 0);
        AssertIsNotNullAndIsNotEmpty(connectResponse.WebSocketSessionId, connectResponse.AccountNumbers[0]);
    }

    [Test]
    public void DeserializeConnectResponseStatusError()
    {
        var connectResponseJson = "{\"status\":\"error\",\"action\":\"connect\",\"web-socket-session-id\":\"423f58ac\",\"message\":\"failed\"}";

        var connectResponse = connectResponseJson.DeserializeKebabCase<ConnectResponse>();

        Assert.AreEqual(Status.Error, connectResponse.Status);
        Assert.IsNotEmpty(connectResponse.Message);
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