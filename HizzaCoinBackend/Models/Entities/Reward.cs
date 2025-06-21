using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HizzaCoinBackend.Models;

public class Reward
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("RequiredStreak")]
    [JsonPropertyName("RequiredStreak")]
    public int RequiredStreak { get; set; }
    
    [BsonElement("RewardedAmount")]
    [JsonPropertyName("RewardedAmount")]
    public int RewardedAmount { get; set; }
    
}