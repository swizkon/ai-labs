using System.Text.Json.Serialization;

namespace BroadcastParser;

public sealed class BroadcastRequest
{
    [JsonPropertyName("mixNumber")]
    public string MixNumber { get; set; } = "";
}
