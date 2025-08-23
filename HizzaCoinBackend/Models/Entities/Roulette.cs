using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HizzaCoinBackend.Models;

public class Roulette
{
    public Roulette(long betNumber, long rolledNumber, RouletteType betType)
    {
        BetNumber = betNumber;
        RolledNumber = rolledNumber;
        BetType = betType;
    }
    
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("WageredTransactionId")]
    [JsonPropertyName("WageredTransactionId")]
    public string? WageredTransactionId { get; set; }
    
    [BsonElement("RewardTransactionId")]
    [JsonPropertyName("RewardTransactionId")]
    public string? RewardTransactionId { get; set; }
    
    [BsonElement("BetNumber")]
    [JsonPropertyName("BetNumber")]
    public long BetNumber { get; set; }
    
    [BsonElement("RolledNumber")]
    [JsonPropertyName("RolledNumber")]
    public long RolledNumber { get; set; }
    
    [BsonElement("BetType")]
    [JsonPropertyName("BetType")]
    public RouletteType BetType { get; set; }
}