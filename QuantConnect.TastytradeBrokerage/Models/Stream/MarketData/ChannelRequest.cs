using Newtonsoft.Json;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;
using QuantConnect.Brokerages.Tastytrade.Serialization;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.MarketData;

/// <summary>
/// Represents a request to open a new communication channel over a WebSocket connection.
/// Typically used to subscribe to a specific service, such as a data feed.
/// </summary>
public readonly struct ChannelRequest
{
    /// <summary>
    /// Gets the event type for this message, which is always <see cref="EventType.ChannelRequest"/>.
    /// </summary>
    public EventType Type => EventType.ChannelRequest;

    /// <summary>
    /// Gets the channel number for the request.
    /// The channel number can be any positive integer chosen by the client to identify this channel.
    /// </summary>
    public int Channel => 1;

    /// <summary>
    /// Gets the name of the service to be accessed via the new channel.
    /// </summary>
    public string Service => "FEED";

    /// <summary>
    /// Gets the parameters required to initialize the channel request.
    /// In this case, a contract identifier is specified.
    /// </summary>
    public object Parameters => new { contract = "AUTO" };

    /// <summary>
    /// Serializes the <see cref="ChannelRequest"/> to a JSON string using camelCase property naming.
    /// </summary>
    /// <returns>A JSON-formatted string representing the channel request message.</returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, JsonSettings.CamelCase);
    }
}
