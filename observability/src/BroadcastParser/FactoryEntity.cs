using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BroadcastParser;

public sealed class FactoryEntity
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("mix_number")]
    public string MixNumber { get; set; } = "";

    [BsonElement("entity_type")]
    public string EntityType { get; set; } = "";

    [BsonElement("created_at_utc")]
    public DateTime CreatedAtUtc { get; set; }
}
