using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HizzaCoinBackend.Models;

public class Reward
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("Streak")]
    [JsonPropertyName("Streak")]
    public int Streak { get; set; }
    
    [BsonElement("RewardedAmount")]
    [JsonPropertyName("RewardedAmount")]
    public int RewardedAmount { get; set; }
    
}