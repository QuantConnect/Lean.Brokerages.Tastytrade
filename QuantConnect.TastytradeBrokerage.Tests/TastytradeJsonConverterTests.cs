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
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Brokerages.Tastytrade.Models;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Serialization;
using QuantConnect.Brokerages.Tastytrade.Models.Stream;
using QuantConnect.Brokerages.Tastytrade.Models.Orders;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

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

    [Test]
    public void SerializeKeepAliveMessage()
    {
        var keepAliveJson = new KeepAlive().ToJson();
        Assert.AreEqual("{\"type\":\"KEEPALIVE\",\"channel\":0}", keepAliveJson);
    }

    [Test]
    public void SerializeSetupConnectionMessage()
    {
        var setupConnectionJson = new SetupConnection().ToJson();
        Assert.AreEqual("{\"type\":\"SETUP\",\"channel\":0,\"version\":\"0.1-DXF-JS/0.3.0\",\"keepaliveTimeout\":60,\"acceptKeepaliveTimeout\":60}", setupConnectionJson);
    }

    [Test]
    public void SerializeAuthorizationMessage()
    {
        var authorization = new Authorization("<redacted>").ToJson();
        Assert.AreEqual("{\"type\":\"AUTH\",\"channel\":0,\"token\":\"<redacted>\"}", authorization);
    }

    [TestCase("{\"type\":\"AUTH_STATE\",\"channel\":0,\"state\":\"UNAUTHORIZED\"}", AuthorizationState.Unauthorized, null)]
    [TestCase("{\"type\":\"AUTH_STATE\",\"channel\":0,\"state\":\"AUTHORIZED\",\"userId\":\"<redacted>\"}", AuthorizationState.Authorized, "<redacted>")]
    public void DeserializeAuthorizationResponse(string authorizationResponseJson, AuthorizationState expectedAuthorizationState, string expectedUserId)
    {
        var authorizationResponse = authorizationResponseJson.DeserializeCamelCase<AuthorizationResponse>();

        Assert.AreEqual(EventType.AuthorizationState, authorizationResponse.Type);
        Assert.AreEqual(0, authorizationResponse.Channel);
        Assert.AreEqual(expectedAuthorizationState, authorizationResponse.State);
        Assert.AreEqual(expectedUserId, authorizationResponse.UserId);
    }

    [Test]
    public void SerializeChannelRequestMessage()
    {
        var channelRequest = new ChannelRequest().ToJson();
        Assert.AreEqual("{\"type\":\"CHANNEL_REQUEST\",\"channel\":1,\"service\":\"FEED\",\"parameters\":{\"contract\":\"AUTO\"}}", channelRequest);
    }

    [Test]
    public void DeserializeErrorStreamResponse()
    {
        var errorResponseJson = "{\"type\":\"ERROR\",\"channel\": 0,\"error\":\"BAD_ACTION\",\"message\":\"Protocol violation with an even channel usage.\"}";

        var errorResponse = errorResponseJson.DeserializeCamelCase<ErrorStreamResponse>();

        Assert.AreEqual(EventType.Error, errorResponse.Type);
        Assert.AreEqual(0, errorResponse.Channel);
        AssertIsNotNullAndIsNotEmpty(errorResponse.Error, errorResponse.Message);
    }

    [Test]
    public void SerializeFeedSetupRequestMessage()
    {
        var feedSetupJson = new FeedSetup().ToJson();

        Assert.AreEqual("{\"type\":\"FEED_SETUP\",\"channel\":1,\"acceptDataFormat\":\"FULL\",\"acceptEventFields\":{\"Quote\":[\"eventSymbol\",\"bidPrice\",\"askPrice\",\"bidSize\",\"askSize\"],\"Trade\":[\"eventSymbol\",\"price\",\"size\",\"time\"]}}", feedSetupJson);
    }

    [Test]
    public void SerializeFeedSubscriptionMessage()
    {
        var tickers = new List<string>() { "AAPL" };

        var feedSubscription = new FeedSubscription(tickers).ToJson();

        AssertIsNotNullAndIsNotEmpty(feedSubscription);
        Assert.AreEqual("{\"type\":\"FEED_SUBSCRIPTION\",\"channel\":1,\"add\":[{\"symbol\":\"AAPL\",\"type\":\"Trade\"},{\"symbol\":\"AAPL\",\"type\":\"Quote\"}]}", feedSubscription);
    }

    [Test]
    public void SerializeFeedUnSubscriptionMessage()
    {
        var tickers = new List<string>() { "AAPL" };

        var feedUnSubscription = new FeedUnSubscription(tickers).ToJson();

        AssertIsNotNullAndIsNotEmpty(feedUnSubscription);
        Assert.AreEqual("{\"type\":\"FEED_SUBSCRIPTION\",\"channel\":1,\"remove\":[{\"symbol\":\"AAPL\",\"type\":\"Trade\"},{\"symbol\":\"AAPL\",\"type\":\"Quote\"}]}", feedUnSubscription);
    }

    [Test]
    public void SerializeFeedSubscriptionMessageWithFiveTickers()
    {
        var tickers = new List<string>() { "AAPL", "INTL", "META", "TSLA", "GOOGL" };

        var feedSubscription = new FeedSubscription(tickers).ToJson();

        AssertIsNotNullAndIsNotEmpty(feedSubscription);
    }

    [Test]
    public void DeserializeFeedDataTradeStreamResponse()
    {
        var feedDataResponseJson = @"{
    ""type"": ""FEED_DATA"",
    ""channel"": 1,
    ""data"": [
        ""Trade"",
        [
            ""AMZN"",
            201.5417,
            257.0,
            1748020169744,
            ""TSLA"",
            340.89,
            100.0,
            1748020169011,
            ""AAPL"",
            196.1,
            200.0,
            1748020168804
        ]
    ]
}";
        var feedData = feedDataResponseJson.DeserializeCamelCase<FeedData>();

        Assert.IsNotNull(feedData);
        Assert.AreEqual(EventType.FeedData, feedData.Type);
        Assert.AreEqual(1, feedData.Channel);
        Assert.AreEqual(MarketDataEvent.Trade, feedData.Data.EventType);
        Assert.AreEqual(3, feedData.Data.Content.Count);
        Assert.IsInstanceOf<IReadOnlyCollection<TradeContent>>(feedData.Data.Content);
        foreach (var trade in feedData.Data.Content.Cast<TradeContent>())
        {
            AssertIsNotNullAndIsNotEmpty(trade.Symbol);
            Assert.Greater(trade.Price, 0);
            Assert.Greater(trade.Size, 0);
            Assert.AreNotEqual(default, trade.TradeDateTime);
        }
    }

    [Test]
    public void DeserializeFeedDataQuoteStreamResponse()
    {
        var feedDataResponseJson = @"{
    ""type"": ""FEED_DATA"",
    ""channel"": 1,
    ""data"": [
        ""Quote"",
        [
            ""NVDA"",
            131.66,
            131.67,
            760.0,
            307.0,
            ""PLTR"",
            124.05,
            124.07,
            275.0,
            326.0
        ]
    ]
}";
        var feedData = feedDataResponseJson.DeserializeCamelCase<FeedData>();

        Assert.IsNotNull(feedData);
        Assert.AreEqual(EventType.FeedData, feedData.Type);
        Assert.AreEqual(1, feedData.Channel);
        Assert.AreEqual(MarketDataEvent.Quote, feedData.Data.EventType);
        Assert.AreEqual(2, feedData.Data.Content.Count);
        Assert.IsInstanceOf<IReadOnlyCollection<QuoteContent>>(feedData.Data.Content);
        foreach (var quote in feedData.Data.Content.Cast<QuoteContent>())
        {
            AssertIsNotNullAndIsNotEmpty(quote.Symbol);
            Assert.Greater(quote.AskPrice, 0);
            Assert.Greater(quote.BidPrice, 0);
            Assert.Greater(quote.AskSize, 0);
            Assert.Greater(quote.BidSize, 0);
        }
    }

    [Test]
    public void DeserializeFutureResponse()
    {
        var instrumentFutureResponseJson = @"{
    ""data"": {
        ""active"": true,
        ""active-month"": false,
        ""back-month-first-calendar-symbol"": false,
        ""closing-only-date"": ""2025-05-23"",
        ""contract-size"": ""42000.0"",
        ""display-factor"": ""0.0001"",
        ""exchange"": ""CME"",
        ""exchange-symbol"": ""RBM5"",
        ""expiration-date"": ""2025-05-30"",
        ""expires-at"": ""2025-05-30T18:30:00.000+00:00"",
        ""first-notice-date"": ""2025-06-03"",
        ""product-group"": ""NYMEX_ENERGY"",
        ""is-closing-only"": true,
        ""last-trade-date"": ""2025-05-30"",
        ""main-fraction"": ""0.0"",
        ""next-active-month"": false,
        ""notional-multiplier"": ""42000.0"",
        ""product-code"": ""RB"",
        ""roll-target-symbol"": ""/RBN5"",
        ""stops-trading-at"": ""2025-05-30T18:30:00.000+00:00"",
        ""streamer-exchange-code"": ""XNYM"",
        ""streamer-symbol"": ""/RBM25:XNYM"",
        ""sub-fraction"": ""0.0"",
        ""symbol"": ""/RBM5"",
        ""tick-size"": ""0.0001"",
        ""is-tradeable"": true,
        ""future-product"": {
            ""active-months"": [
                ""F"",
                ""G"",
                ""H"",
                ""J"",
                ""K"",
                ""M"",
                ""N"",
                ""Q"",
                ""U"",
                ""V"",
                ""X"",
                ""Z""
            ],
            ""back-month-first-calendar-symbol"": false,
            ""cash-settled"": false,
            ""clearing-code"": ""RB"",
            ""clearing-exchange-code"": ""07"",
            ""clearport-code"": ""RB"",
            ""code"": ""RB"",
            ""description"": ""RBOB Gasoline Futures"",
            ""display-factor"": ""0.0001"",
            ""exchange"": ""CME"",
            ""first-notice"": false,
            ""legacy-code"": ""RB"",
            ""legacy-exchange-code"": ""NYM"",
            ""listed-months"": [
                ""F"",
                ""G"",
                ""H"",
                ""J"",
                ""K"",
                ""M"",
                ""N"",
                ""Q"",
                ""U"",
                ""V"",
                ""X"",
                ""Z""
            ],
            ""market-sector"": ""Energy"",
            ""notional-multiplier"": ""42000.0"",
            ""price-format"": ""decimal"",
            ""product-type"": ""Physical"",
            ""security-group"": ""CL"",
            ""small-notional"": false,
            ""streamer-exchange-code"": ""XNYM"",
            ""supported"": true,
            ""root-symbol"": ""/RB"",
            ""tick-size"": ""0.0001"",
            ""roll"": {
                ""active-count"": 13,
                ""business-days-offset"": 5,
                ""cash-settled"": false,
                ""first-notice"": false,
                ""name"": ""energies""
            }
        },
        ""tick-sizes"": [
            {
                ""value"": ""0.0001""
            }
        ]
    },
    ""context"": ""/instruments/futures/RBM5""
}";

        var instrumentFuture = instrumentFutureResponseJson.DeserializeKebabCase<BaseResponse<Future>>();

        Assert.IsNotNull(instrumentFuture);
        AssertIsNotNullAndIsNotEmpty(instrumentFuture.Context, instrumentFuture.Data.Symbol, instrumentFuture.Data.StreamerSymbol);
    }

    [Test]
    public void SerializeLegAttributes()
    {
        var expectedLegAttributes = @"{""action"":""Buy to Open"",""instrument-type"":""Equity Option"",""quantity"":1.0,""symbol"":""AAPL  230818C00197500""}";

        var legAttributes = new LegAttributes(OrderAction.BuyToOpen, InstrumentType.EquityOption, 1m, "AAPL  230818C00197500");

        var actualLegAttributes = JsonConvert.SerializeObject(legAttributes, JsonSettings.KebabCase);

        Assert.AreEqual(expectedLegAttributes, actualLegAttributes);
    }

    [TestCase(OrderType.Market, InstrumentType.Equity, OrderAction.BuyToOpen, TimeInForce.Day, null, null, null, null)]
    [TestCase(OrderType.Market, InstrumentType.EquityOption, OrderAction.Sell, TimeInForce.GoodTilDate, "2025/05/30", null, null, null)]
    [TestCase(OrderType.Limit, InstrumentType.Future, OrderAction.BuyToOpen, TimeInForce.GoodTillCancel, null, 100, null, Orders.OrderDirection.Buy)]
    [TestCase(OrderType.Limit, InstrumentType.FutureOption, OrderAction.SellToClose, TimeInForce.GoodTillCancel, null, 200, null, Orders.OrderDirection.Sell)]
    [TestCase(OrderType.Stop, InstrumentType.Equity, OrderAction.BuyToOpen, TimeInForce.GoodTilDate, "2025/05/30", null, 210, null)]
    [TestCase(OrderType.Stop, InstrumentType.EquityOption, OrderAction.SellToOpen, TimeInForce.GoodTillCancel, null, null, 190, null)]
    [TestCase(OrderType.Stop, InstrumentType.Equity, OrderAction.Buy, TimeInForce.Day, null, null, 190, null)]
    [TestCase(OrderType.StopLimit, InstrumentType.Equity, OrderAction.Buy, TimeInForce.GoodTilDate, "2025/05/30", 180, 190, Orders.OrderDirection.Buy)]
    [TestCase(OrderType.StopLimit, InstrumentType.EquityOption, OrderAction.SellToOpen, TimeInForce.Day, null, 200, 190, Orders.OrderDirection.Sell)]
    public void SerializeVariousOrderTypeRequestMessage(OrderType orderType, InstrumentType instrumentType, OrderAction legOrderAction, TimeInForce timeInForce, DateTime? expiryDateTime, decimal? limitPrice, decimal? stopPrice, Orders.OrderDirection? leanOrderDirection)
    {
        var legAttributes = new List<LegAttributes> { new LegAttributes(legOrderAction, instrumentType, 1m, "AAPL  230818C00197500") };

        var order = default(OrderBaseRequest);
        switch (orderType)
        {
            case OrderType.Market:
                order = new MarketOrderRequest(timeInForce, expiryDateTime, legAttributes);
                break;
            case OrderType.Limit:
                order = new LimitOrderRequest(timeInForce, expiryDateTime, legAttributes, limitPrice.Value, leanOrderDirection.Value);
                break;
            case OrderType.Stop:
                order = new StopMarketOrderRequest(timeInForce, expiryDateTime, legAttributes, stopPrice.Value);
                break;
            case OrderType.StopLimit:
                order = new StopLimitOrderRequest(timeInForce, expiryDateTime, legAttributes, limitPrice.Value, stopPrice.Value, leanOrderDirection.Value);
                break;
            default:
                throw new NotSupportedException();
        }

        var orderJson = order.ToJson();

        AssertIsNotNullAndIsNotEmpty(orderJson);
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