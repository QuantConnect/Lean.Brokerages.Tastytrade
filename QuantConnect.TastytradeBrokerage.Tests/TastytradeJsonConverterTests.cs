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
using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Brokerages.Authentication;
using QuantConnect.Brokerages.Tastytrade.Models;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Serialization;
using QuantConnect.Brokerages.Tastytrade.Models.Stream;
using QuantConnect.Brokerages.Tastytrade.Models.Orders;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.Base;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;
using QuantConnect.Brokerages.Tastytrade.Models.Stream.AccountData;

namespace QuantConnect.Brokerages.Tastytrade.Tests;

[TestFixture]
public class TastytradeJsonConverterTests
{
    [Test]
    public void ReturnsCorrectJsonRepresentationForValidCreateSession()
    {
        var createSession = new CreateSessionRequest("ramses", "pharaoh");

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
    public void DeserializeErrorMessageFromJsonFile()
    {
        var jsonContent = File.ReadAllText(Path.Combine("TestData", "Error_Responses.json"));

        var errorResponses = jsonContent.DeserializeKebabCase<IReadOnlyCollection<ErrorResponse>>();

        foreach (var response in errorResponses)
        {
            AssertIsNotNullAndIsNotEmpty(response.Error.Code, response.Error.Message, response.Error.ToString());
            if (response.Error.Errors != null)
            {
                foreach (var nestedError in response.Error.Errors)
                {
                    Assert.IsNull(nestedError.Errors);
                    AssertIsNotNullAndIsNotEmpty(nestedError.Code, nestedError.Message);
                }
            }
        }
    }

    [Test]
    public void DeserializeCreateSessionResponseFromJsonFile()
    {
        var jsonContent = File.ReadAllText(Path.Combine("TestData", "Create_Session_Response.json"));

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
        var jsonContent = File.ReadAllText(Path.Combine("TestData", "Get_Positions.json"));

        var positions = jsonContent.DeserializeKebabCase<BaseResponse<ResponseList<Position>>>().Data.Items;

        Assert.IsNotNull(positions);
    }

    [Test]
    public void DeserializeGetApiQuoteToken()
    {
        var jsonContent = File.ReadAllText(Path.Combine("TestData", "Get_Api_Quote_Token.json"));

        var apiQuoteTokenResponse = jsonContent.DeserializeKebabCase<BaseResponse<ApiQuoteTokenResponse>>();

        Assert.IsNotNull(apiQuoteTokenResponse);
        Assert.IsNotNull(apiQuoteTokenResponse.Data);
        AssertIsNotNullAndIsNotEmpty(apiQuoteTokenResponse.Data.DxlinkUrl, apiQuoteTokenResponse.Data.Level, apiQuoteTokenResponse.Data.Token);
        Assert.AreEqual("/api-quote-tokens", apiQuoteTokenResponse.Context);
    }

    [Test]
    public void SerializeStreamHeartbeatMessage()
    {
        var heartbeatJson = new HeartbeatRequest(TokenType.SessionToken, "your session token here", 1).ToJson();

        Assert.AreEqual("{\"action\":\"heartbeat\",\"auth-token\":\"your session token here\",\"request-id\":1}", heartbeatJson);
    }

    [Test]
    public void SerializeStreamHeartbeatMessageWithBearerToken()
    {
        var heartbeatJson = new HeartbeatRequest(TokenType.Bearer, "your session token here", 1).ToJson();

        Assert.AreEqual("{\"action\":\"heartbeat\",\"auth-token\":\"Bearer your session token here\",\"request-id\":1}", heartbeatJson);
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
        var connectJson = new ConnectRequest(TokenType.SessionToken, "your session token here", 1, "12345").ToJson();

        Assert.AreEqual("{\"action\":\"connect\",\"value\":[\"12345\"],\"auth-token\":\"your session token here\",\"request-id\":1}", connectJson);
    }

    [Test]
    public void SerializeStreamConnectMessageWithBearerToken()
    {
        var connectJson = new ConnectRequest(TokenType.Bearer, "your session token here", 1, "12345").ToJson();

        Assert.AreEqual("{\"action\":\"connect\",\"value\":[\"12345\"],\"auth-token\":\"Bearer your session token here\",\"request-id\":1}", connectJson);
    }

    [Test]
    public void DeserializeConnectResponseStatusOk()
    {
        var connectResponseJson = "{\"status\":\"ok\",\"action\":\"connect\",\"web-socket-session-id\":\"c8531fa0\",\"value\":[\"5WX06827\"],\"request-id\":1}";

        var connectResponse = connectResponseJson.DeserializeKebabCase<ConnectResponse>();

        Assert.IsInstanceOf<BaseAccountMaintenanceStatus>(connectResponse);
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
        var keepAliveJson = new KeepAliveRequest().ToJson();
        Assert.AreEqual("{\"type\":\"KEEPALIVE\",\"channel\":0}", keepAliveJson);
    }

    [Test]
    public void SerializeSetupConnectionMessage()
    {
        var setupConnectionJson = new SetupConnectionRequest().ToJson();
        Assert.AreEqual("{\"type\":\"SETUP\",\"channel\":0,\"version\":\"0.1-DXF-JS/0.3.0\",\"keepaliveTimeout\":60,\"acceptKeepaliveTimeout\":60}", setupConnectionJson);
    }

    [Test]
    public void SerializeAuthorizationMessage()
    {
        var authorization = new AuthorizationRequest("<redacted>").ToJson();
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
        var feedSetupJson = new FeedSetupRequest().ToJson();

        Assert.AreEqual("{\"type\":\"FEED_SETUP\",\"channel\":1,\"acceptDataFormat\":\"COMPACT\",\"acceptEventFields\":{\"Quote\":[\"eventSymbol\",\"bidPrice\",\"askPrice\",\"bidSize\",\"askSize\"],\"Trade\":[\"eventSymbol\",\"price\",\"size\",\"time\"],\"Summary\":[\"eventSymbol\",\"openInterest\"],\"Candle\":[\"eventFlags\",\"eventSymbol\",\"time\",\"open\",\"high\",\"low\",\"close\",\"volume\",\"openInterest\"]}}", feedSetupJson);
    }

    [Test]
    public void SerializeFeedSubscriptionMessage()
    {
        var symbolMapper = new TastytradeBrokerageSymbolMapper(default);

        var symbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);

        var feedSubscription = new FeedSubscription([symbol], symbolMapper).ToJson();

        AssertIsNotNullAndIsNotEmpty(feedSubscription);
        Assert.AreEqual("{\"add\":[{\"symbol\":\"AAPL\",\"type\":\"Trade\"},{\"symbol\":\"AAPL\",\"type\":\"Quote\"}],\"type\":\"FEED_SUBSCRIPTION\",\"channel\":1}", feedSubscription);
    }

    [Test]
    public void SerializeFeedUnSubscriptionMessage()
    {
        var symbolMapper = new TastytradeBrokerageSymbolMapper(default);

        var symbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);

        var feedUnSubscription = new FeedUnSubscription([symbol], symbolMapper).ToJson();

        AssertIsNotNullAndIsNotEmpty(feedUnSubscription);
        Assert.AreEqual("{\"remove\":[{\"symbol\":\"AAPL\",\"type\":\"Trade\"},{\"symbol\":\"AAPL\",\"type\":\"Quote\"}],\"type\":\"FEED_SUBSCRIPTION\",\"channel\":1}", feedUnSubscription);
    }

    [Test]
    public void SerializeFeedSubscriptionMessageWithFiveTickers()
    {
        var symbolMapper = new TastytradeBrokerageSymbolMapper(default);
        var symbols = new List<Symbol>();
        foreach (var ticker in new string[] { "AAPL", "INTL", "META", "TSLA", "GOOGL" })
        {
            symbols.Add(Symbol.Create(ticker, SecurityType.Equity, Market.USA));
        }

        var feedSubscription = new FeedSubscription(symbols, symbolMapper).ToJson();

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
            1748020168804,
            ""SPX"",
            5931.91,
            ""NaN"",
            1748892788016,
            ""./ESU25P900:XCME"",
            ""NaN"",
            ""NaN"",
            0
        ]
    ]
}";
        var feedData = feedDataResponseJson.DeserializeCamelCase<FeedData>();

        Assert.IsNotNull(feedData);
        Assert.AreEqual(EventType.FeedData, feedData.Type);
        Assert.AreEqual(1, feedData.Channel);
        Assert.AreEqual(MarketDataEvent.Trade, feedData.Data.EventType);
        Assert.AreEqual(5, feedData.Data.Content.Count);
        Assert.IsInstanceOf<IReadOnlyCollection<TradeContent>>(feedData.Data.Content);
        foreach (var trade in feedData.Data.Content.Cast<TradeContent>())
        {
            AssertIsNotNullAndIsNotEmpty(trade.Symbol);
            Assert.GreaterOrEqual(trade.Price, 0);
            Assert.GreaterOrEqual(trade.Size, 0);
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

    [TestCase("[\".AAPL250620C200\",34905]")]
    [TestCase("[\"SPY\",0]")]
    [TestCase("[\"/ESM25:XCME\",2017117]")]
    [TestCase("[\"AAPL\",0]")]
    public void DeserializeFeedDataSummaryStreamResponse(string summaryArrayData)
    {
        var feedDataResponseJson = $@"{{""type"":""FEED_DATA"",""channel"":1,""data"":[""Summary"",{summaryArrayData}]}}";

        var feedData = feedDataResponseJson.DeserializeCamelCase<FeedData>();

        Assert.IsNotNull(feedData);
        Assert.AreEqual(1, feedData.Data.Content.Count);
        foreach (var summary in feedData.Data.Content.Cast<SummaryContent>())
        {
            AssertIsNotNullAndIsNotEmpty(summary.Symbol);
            Assert.GreaterOrEqual(summary.OpenInterest, 0);
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
    [TestCase(OrderType.Market, InstrumentType.EquityOption, OrderAction.Sell, TimeInForce.Day, null, null, null, null)]
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
                order = new MarketOrderRequest(legAttributes);
                break;
            case OrderType.Limit:
                order = new LimitOrderRequest(timeInForce, expiryDateTime, legAttributes, limitPrice.Value, leanOrderDirection.Value);
                break;
            case OrderType.Stop:
                order = new StopMarketOrderRequest(timeInForce, expiryDateTime, legAttributes, stopPrice.Value, instrumentType);
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

    [Test]
    public void DeserializeSubmitOrderResponse()
    {
        var jsonContent = @"{
    ""data"": {
        ""order"": {
            ""id"": 270561,
            ""account-number"": ""5WX06827"",
            ""cancellable"": false,
            ""editable"": false,
            ""edited"": false,
            ""global-request-id"": ""f6c5863356cf1be83fed349d54ee4862"",
            ""order-type"": ""Market"",
            ""received-at"": ""2025-05-28T13:38:47.851+00:00"",
            ""size"": 1,
            ""status"": ""Routed"",
            ""time-in-force"": ""Day"",
            ""underlying-instrument-type"": ""Equity"",
            ""underlying-symbol"": ""AAPL"",
            ""updated-at"": 1748439528049,
            ""legs"": [
                {
                    ""action"": ""Buy to Open"",
                    ""instrument-type"": ""Equity"",
                    ""quantity"": 1,
                    ""remaining-quantity"": 1,
                    ""symbol"": ""AAPL"",
                    ""fills"": []
                }
            ]
        },
        ""warnings"": []
    },
    ""context"": ""/accounts/5WX06827/orders""
}";

        var orderResponse = jsonContent.DeserializeKebabCase<BaseResponse<OrderResponse>>();

        var order = orderResponse.Data.Order;

        AssertIsNotNullAndIsNotEmpty(order.Id);
        Assert.IsNotNull(order.Legs);
        Assert.AreEqual(1, order.Legs.Count);
        Assert.AreEqual(OrderStatus.Routed, order.Status);

        var leg = order.Legs.First();
        Assert.AreEqual(OrderAction.BuyToOpen, leg.Action);
        Assert.AreEqual(1m, leg.Quantity);
        Assert.AreEqual(1m, leg.RemainingQuantity);
        Assert.AreEqual(0, leg.Fills.Count);
    }

    [Test]
    public void DeserializeStreamFilledOrderMessage()
    {
        var jsonContent = @"{
    ""type"": ""Order"",
    ""data"": {
        ""id"": 270602,
        ""account-number"": ""5WX06827"",
        ""cancellable"": false,
        ""editable"": false,
        ""edited"": false,
        ""ext-client-order-id"": ""0a000000d40004210a"",
        ""ext-exchange-order-number"": ""910533337354"",
        ""ext-global-order-number"": 212,
        ""global-request-id"": ""cac193dce95dd480f7c191e8655f54a9"",
        ""order-type"": ""Market"",
        ""received-at"": ""2025-05-28T16:00:01.651+00:00"",
        ""size"": 1,
        ""status"": ""Filled"",
        ""terminal-at"": ""2025-05-28T16:00:02.550+00:00"",
        ""time-in-force"": ""Day"",
        ""underlying-instrument-type"": ""Equity"",
        ""underlying-symbol"": ""AAPL"",
        ""updated-at"": 1748448002553,
        ""legs"": [
            {
                ""action"": ""Buy to Open"",
                ""instrument-type"": ""Equity"",
                ""quantity"": 1,
                ""remaining-quantity"": 0,
                ""symbol"": ""AAPL"",
                ""fills"": [
                    {
                        ""destination-venue"": ""TEST_A"",
                        ""ext-exec-id"": ""134"",
                        ""ext-group-fill-id"": ""0"",
                        ""fill-id"": ""2_TW::TEST_A1::20250528.135-TEST_FILL"",
                        ""fill-price"": ""1.0"",
                        ""filled-at"": ""2025-05-28T16:00:01.953+00:00"",
                        ""quantity"": 1
                    }
                ]
            }
        ]
    },
    ""timestamp"": 1748448002620
}";

        var accountData = jsonContent.DeserializeKebabCase<AccountData>();

        Assert.AreEqual(EventType.Order, accountData.Type);

        var order = accountData.Order;
        Assert.AreEqual(OrderStatus.Filled, order.Status);
        AssertIsNotNullAndIsNotEmpty(order.Id);
        Assert.AreNotEqual(default, order.ReceivedAtUtc);
        Assert.AreEqual(DateTimeKind.Utc, order.ReceivedAtUtc.Kind);

        Assert.AreEqual(1, order.Legs.Count);
        var leg = accountData.Order.Legs.First();

        Assert.AreEqual(OrderAction.BuyToOpen, leg.Action);
        Assert.AreEqual(1m, leg.Quantity);
        Assert.AreEqual(0m, leg.RemainingQuantity);

        Assert.AreEqual(1m, leg.Fills.Count);
        var fill = leg.Fills.First();
        Assert.AreEqual(1m, fill.Quantity);
        Assert.AreEqual(1m, fill.FillPrice);
        Assert.AreNotEqual(default, fill.FilledAt);
    }

    [Test]
    public void DeserializeRestOpenOrderResponse()
    {
        var jsonContent = @"{
    ""data"": {
        ""items"": [
{
                ""id"": 270976,
                ""account-number"": ""5WX06827"",
                ""cancellable"": true,
                ""editable"": true,
                ""edited"": false,
                ""ext-client-order-id"": ""80000000b700042280"",
                ""ext-exchange-order-number"": ""785979286144"",
                ""ext-global-order-number"": 183,
                ""global-request-id"": ""c73256d0ea9995bde1d4b8e75f8ee549"",
                ""order-type"": ""Stop Limit"",
                ""price"": ""4.0"",
                ""price-effect"": ""Debit"",
                ""received-at"": ""2025-05-30T13:55:00.653+00:00"",
                ""size"": 1,
                ""source"": ""QuantConnect"",
                ""status"": ""Live"",
                ""stop-trigger"": ""2.0"",
                ""time-in-force"": ""GTC"",
                ""underlying-instrument-type"": ""Equity"",
                ""underlying-symbol"": ""AAPL"",
                ""updated-at"": 1748613301843,
                ""legs"": [
                    {
                        ""action"": ""Buy to Open"",
                        ""instrument-type"": ""Equity"",
                        ""quantity"": 1,
                        ""remaining-quantity"": 1,
                        ""symbol"": ""AAPL"",
                        ""fills"": []
                    }
                ]
            }
        ]
    },
    ""context"": ""/accounts/5WX06827/orders"",
    ""pagination"": {
        ""per-page"": 100,
        ""page-offset"": 0,
        ""item-offset"": 0,
        ""total-items"": 1,
        ""total-pages"": 1,
        ""current-item-count"": 1,
        ""previous-link"": null,
        ""next-link"": null,
        ""paging-link-template"": null
    }
}";

        var orders = jsonContent.DeserializeKebabCase<BaseResponse<ResponseList<Order>>>().Data.Items;

        Assert.AreEqual(1, orders.Count);
        foreach (var order in orders)
        {
            Assert.AreEqual("270976", order.Id);
            Assert.AreEqual(OrderType.StopLimit, order.OrderType);
            Assert.AreEqual(4m, order.Price);
            Assert.AreEqual(OrderStatus.Live, order.Status);
            Assert.AreEqual(2m, order.StopTrigger);
            Assert.AreEqual(TimeInForce.GoodTillCancel, order.TimeInForce);
            Assert.AreEqual("AAPL", order.UnderlyingSymbol);
            Assert.AreNotEqual(default, order.ReceivedAt);

            Assert.AreEqual(1, order.Legs.Count);
            foreach (var leg in order.Legs)
            {
                Assert.AreEqual(OrderAction.BuyToOpen, leg.Action);
                Assert.AreEqual(InstrumentType.Equity, leg.InstrumentType);
                Assert.AreEqual(1, leg.Quantity);
                Assert.AreEqual(1, leg.RemainingQuantity);
                Assert.AreEqual("AAPL", leg.Symbol);
                Assert.AreEqual(0, leg.Fills.Count);
            }
        }
    }

    [Test]
    public void DeserializeRestOpenOrderResponseWithGoodTilDateTimeInForce()
    {
        var jsonContent = @"{
    ""data"": {
        ""items"": [
            {
                ""id"": 271261,
                ""account-number"": ""5WX06827"",
                ""cancellable"": true,
                ""editable"": true,
                ""edited"": false,
                ""global-request-id"": ""f556ad1d5578bae1bc67cc503f31d9a6"",
                ""gtc-date"": ""2025-06-20"",
                ""order-type"": ""Limit"",
                ""price"": ""4.0"",
                ""price-effect"": ""Debit"",
                ""received-at"": ""2025-06-01T20:24:06.428+00:00"",
                ""size"": 1,
                ""source"": ""QuantConnect"",
                ""status"": ""Received"",
                ""time-in-force"": ""GTD"",
                ""underlying-instrument-type"": ""Equity"",
                ""underlying-symbol"": ""AAPL"",
                ""updated-at"": 1748809446428,
                ""legs"": [
                    {
                        ""action"": ""Buy to Open"",
                        ""instrument-type"": ""Equity"",
                        ""quantity"": 1,
                        ""remaining-quantity"": 1,
                        ""symbol"": ""AAPL"",
                        ""fills"": []
                    }
                ]
            }
        ]
    },
    ""context"": ""/accounts/5WX06827/orders"",
    ""pagination"": {
        ""per-page"": 100,
        ""page-offset"": 0,
        ""item-offset"": 0,
        ""total-items"": 1,
        ""total-pages"": 1,
        ""current-item-count"": 1,
        ""previous-link"": null,
        ""next-link"": null,
        ""paging-link-template"": null
    }
}";

        var orders = jsonContent.DeserializeKebabCase<BaseResponse<ResponseList<Order>>>().Data.Items;

        foreach (var order in orders)
        {
            Assert.AreEqual(TimeInForce.GoodTilDate, order.TimeInForce);
            Assert.AreNotEqual(default, order.GtcDate);
            Assert.AreEqual(new DateTime(2025, 06, 20), order.GtcDate);
        }
    }

    [Test]
    public void DeserializeRestFutureOptionChainResponse()
    {
        var jsonContent = @"{
    ""data"": {
        ""items"": [
            {
                ""active"": true,
                ""days-to-expiration"": 26,
                ""display-factor"": ""0.01"",
                ""exchange"": ""CME"",
                ""exchange-symbol"": ""OGN5 P2095"",
                ""exercise-style"": ""American"",
                ""expiration-date"": ""2025-06-25"",
                ""expires-at"": ""2025-06-25T17:30:00.000+00:00"",
                ""future-price-ratio"": ""1.0"",
                ""is-closing-only"": false,
                ""is-confirmed"": true,
                ""is-exercisable-weekly"": true,
                ""is-primary-deliverable"": true,
                ""is-vanilla"": true,
                ""last-trade-time"": ""0"",
                ""maturity-date"": ""2025-06-25"",
                ""multiplier"": ""1.0"",
                ""notional-value"": ""1.0"",
                ""option-root-symbol"": ""OG"",
                ""option-type"": ""P"",
                ""product-code"": ""GC"",
                ""root-symbol"": ""/GC"",
                ""security-exchange"": ""4"",
                ""settlement-type"": ""Future"",
                ""stops-trading-at"": ""2025-06-25T17:30:00.000+00:00"",
                ""streamer-symbol"": ""./OGN25P2095:XCEC"",
                ""strike-factor"": ""1.0"",
                ""strike-price"": ""2095.0"",
                ""sx-id"": ""0"",
                ""symbol"": ""./GCQ5 OGN5  250625P2095"",
                ""underlying-count"": ""1.0"",
                ""underlying-symbol"": ""/GCQ5"",
                ""future-option-product"": {
                    ""cash-settled"": false,
                    ""clearing-code"": ""37"",
                    ""clearing-exchange-code"": ""04"",
                    ""clearing-price-multiplier"": ""1.0"",
                    ""clearport-code"": ""OG"",
                    ""code"": ""OG"",
                    ""display-factor"": ""0.01"",
                    ""exchange"": ""CME"",
                    ""expiration-type"": ""Regular"",
                    ""is-rollover"": false,
                    ""legacy-code"": ""OG"",
                    ""market-sector"": ""Metals"",
                    ""product-type"": ""Physical"",
                    ""root-symbol"": ""OG"",
                    ""settlement-delay-days"": 0,
                    ""supported"": true
                }
            }
        ]
    },
    ""context"": ""/futures-option-chains/GC""
}";

        var futureOptions = jsonContent.DeserializeKebabCase<BaseResponse<ResponseList<FutureOption>>>().Data.Items;

        Assert.AreEqual(1, futureOptions.Count);
        foreach (var futureOption in futureOptions)
        {
            Assert.AreEqual(new DateTime(2025, 06, 25), futureOption.ExpirationDate);
            Assert.AreEqual(OptionType.Put, futureOption.OptionType);
            Assert.AreEqual("./OGN25P2095:XCEC", futureOption.StreamerSymbol);
            Assert.AreEqual(2095m, futureOption.StrikePrice);
            Assert.AreEqual("./GCQ5 OGN5  250625P2095", futureOption.Symbol);
        }
    }

    [Test]
    public void SerializeCandleFeedSubscriptionMessage()
    {
        var candleFeedSubscription = new CandleFeedSubscription("AAPL", Resolution.Daily, new DateTime(2022, 10, 24)).ToJson();

        AssertIsNotNullAndIsNotEmpty(candleFeedSubscription);
        Assert.AreEqual("{\"add\":[{\"symbol\":\"AAPL{=d}\",\"type\":\"Candle\",\"fromTime\":1666569600000}],\"type\":\"FEED_SUBSCRIPTION\",\"channel\":1}", candleFeedSubscription);
    }

    [Test]
    public void SerializeCandleFeedUnSubscriptionMessage()
    {
        var candleFeedSubscription = new CandleFeedUnsubscription("AAPL", Resolution.Daily, new DateTime(2022, 10, 24)).ToJson();
        AssertIsNotNullAndIsNotEmpty(candleFeedSubscription);
        Assert.AreEqual("{\"remove\":[{\"symbol\":\"AAPL{=d}\",\"type\":\"Candle\",\"fromTime\":1666569600000}],\"type\":\"FEED_SUBSCRIPTION\",\"channel\":1}", candleFeedSubscription);
    }

    [Test]
    public void DeserializeFeedDataCandleStreamResponse()
    {
        var feedDataResponseJson = @"{
    ""type"": ""FEED_DATA"",
    ""channel"": 1,
    ""data"": [
        ""Candle"",
        [
            4,
            ""AAPL{=d}"",
            1750032000000,
            197.3,
            198.685,
            196.5636,
            198.42,
            4.3020691E7,
            ""NaN"",
            0,
            ""AAPL{=d}"",
            1749772800000,
            199.73,
            200.37,
            195.7,
            196.45,
            5.1447349E7,
            ""NaN"",
            10,
            ""AAPL{=d}"",
            926208000000,
            ""NaN"",
            ""NaN"",
            ""NaN"",
            ""NaN"",
            ""NaN"",
            ""NaN""
        ]
    ]
}";

        var feedData = feedDataResponseJson.DeserializeCamelCase<FeedData>();

        Assert.IsNotNull(feedData);
        Assert.AreEqual(EventType.FeedData, feedData.Type);
        Assert.AreEqual(1, feedData.Channel);
        Assert.AreEqual(MarketDataEvent.Candle, feedData.Data.EventType);
        Assert.AreEqual(3, feedData.Data.Content.Count);
        Assert.IsInstanceOf<IReadOnlyCollection<CandleContent>>(feedData.Data.Content);

        var candles = feedData.Data.Content.Cast<CandleContent>();
        Assert.IsTrue(candles.First().EventFlag.HasFlag(EventFlag.SnapshotBegin));
        Assert.AreEqual(EventFlag.RemoveEvent | EventFlag.SnapshotEnd, candles.Last().EventFlag);
        foreach (var candle in candles)
        {
            AssertIsNotNullAndIsNotEmpty(candle.Symbol);
            Assert.IsTrue(candle.Open == null || candle.Open > 0, "Open should be null or greater than zero.");
            Assert.IsTrue(candle.High == null || candle.High > 0, "High should be null or greater than zero.");
            Assert.IsTrue(candle.Low == null || candle.Low > 0, "Low should be null or greater than zero.");
            Assert.IsTrue(candle.Close == null || candle.Close > 0, "Close should be null or greater than zero.");
            Assert.IsTrue(candle.Volume == null || candle.Volume > 0, "Volume should be null or greater than zero.");
            Assert.AreEqual(candle.OpenInterest, null);
            Assert.AreNotEqual(default, candle.DateTime);
        }
    }

    [Test]
    public void SerializeLeanAccessTokenMetaDataRequest()
    {
        var leanAccessTokenJson = new LeanAccessTokenMetaDataRequest("Tastytrade", "zxcvb123", "7DZ64577").ToJson();

        Assert.AreEqual("{\"refreshToken\":\"zxcvb123\",\"brokerage\":\"tastytrade\",\"accountId\":\"7DZ64577\"}", leanAccessTokenJson);
    }

    [Test]
    public void DeserializeLeanAccessTokenMetaDataResponse()
    {
        var jsonContent = @"{
    ""accessToken"": ""zxcvb123"",
    ""tokenType"": ""Bearer"",
    ""expiresIn"": 900,
    ""tokenId"": ""123bvcxz"",
    ""success"": true
}";

        var leanAccessTokenResponse = jsonContent.DeserializeCamelCase<LeanAccessTokenMetaDataResponse>();

        Assert.IsNotNull(leanAccessTokenResponse);
        Assert.AreEqual("zxcvb123", leanAccessTokenResponse.AccessToken);
        Assert.AreEqual(TokenType.Bearer, leanAccessTokenResponse.TokenType);

        var expectedDateTime = DateTime.UtcNow.AddSeconds(900).AddSeconds(-70); //  The 70 second buffer to test time expiration
        Assert.Less(expectedDateTime, leanAccessTokenResponse.Expiration);
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