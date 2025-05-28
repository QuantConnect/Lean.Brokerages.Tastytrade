using Newtonsoft.Json;
using QuantConnect.Brokerages.Tastytrade.Models.Enum;

namespace QuantConnect.Brokerages.Tastytrade.Models.Stream.AccountData;

/// <summary>
/// Represents a basic response message received over a WebSocket connection for account-related data.
/// </summary>
public class BaseResponse
{
    /// <summary>
    /// Gets the type of the event. This is typically used in a <c>switch</c> statement
    /// to determine how the response should be handled.
    /// </summary>
    public EventType Type { get; }

    /// <summary>
    /// Gets the Unix timestamp (in milliseconds) indicating when the response was generated.
    /// </summary>
    public long Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseResponse"/> class.
    /// </summary>
    /// <param name="type">The event type, used to identify and route the message appropriately.</param>
    /// <param name="timestamp">The timestamp indicating when the message was created, in Unix time milliseconds.</param>
    [JsonConstructor]
    public BaseResponse(EventType type, long timestamp) => (Type, Timestamp) = (type, timestamp);
}
