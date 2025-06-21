using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HizzaCoinBackend.Models;

public class Destiny
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("DestinyLevel")]
    [JsonPropertyName("DestinyLevel")]
    public int DestinyLevel { get; set; }
    
    [BsonElement("RewardedAmount")]
    [JsonPropertyName("RewardedAmount")]
    public int RewardedAmount { get; set; }
    
}