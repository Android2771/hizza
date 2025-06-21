using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HizzaCoinBackend.Models;

public class Challenge
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("ChallengerDiscordId")]
    [JsonPropertyName("ChallengerDiscordId")]
    public string ChallengerDiscordId { get; set; }
    
    [BsonElement("ChallengedDiscordId")]
    [JsonPropertyName("ChallengedDiscordId")]
    public string ChallengedDiscordId { get; set; }
    
    [BsonElement("BetAmount")]
    [JsonPropertyName("BetAmount")]
    public int BetAmount { get; set; }
    
    [BsonElement("Date")]
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [BsonElement("ChallengerHand")]
    [JsonPropertyName("ChallengerHand")]
    public Hand ChallengerHand { get; set; }

    [BsonElement("ChallengedHand")]
    [JsonPropertyName("ChallengedHand")]
    public Hand ChallengedHand { get; set; }
    
    [BsonElement("InProgress")]
    [JsonPropertyName("InProgress")]
    public int InProgress { get; set; }
}