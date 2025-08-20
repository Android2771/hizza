using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HizzaCoinBackend.Models;

public class Roulette
{
    public Roulette(int wageredAmount, int rewardedAmount, int betNumber, int rolledNumber, int betType)
    {
        WageredAmount = wageredAmount;
        RewardedAmount = rewardedAmount;
        BetNumber = betNumber;
        RolledNumber = rolledNumber;
        BetType = betType;
    }

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("WageredAmount")]
    [JsonPropertyName("WageredAmount")]
    public int WageredAmount { get; set; }
    
    [BsonElement("RewardedAmount")]
    [JsonPropertyName("RewardedAmount")]
    public int RewardedAmount { get; set; }
    
    [BsonElement("BetNumber")]
    [JsonPropertyName("BetNumber")]
    public int BetNumber { get; set; }
    
    [BsonElement("RolledNumber")]
    [JsonPropertyName("RolledNumber")]
    public int RolledNumber { get; set; }
    
    [BsonElement("BetType")]
    [JsonPropertyName("BetType")]
    public int BetType { get; set; }
}