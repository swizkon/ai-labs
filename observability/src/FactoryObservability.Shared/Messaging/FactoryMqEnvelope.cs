using System.Text.Json.Serialization;

namespace FactoryObservability.Shared.Messaging;

/// <summary>JSON envelope for MQ body (includes trace propagation per docs/SCHEMA.md).</summary>
public sealed class FactoryMqEnvelope
{
    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = "";

    [JsonPropertyName("mix_number")]
    public string MixNumber { get; set; } = "";

    [JsonPropertyName("traceparent")]
    public string TraceParent { get; set; } = "";

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }
}
