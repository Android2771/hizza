using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HizzaCoinBackend.Models;

public class Challenge
{
    public Challenge()
    {
    }

    public Challenge(string challengerDiscordId, string challengedDiscordId, int wager, DateTime date, Hand challengerHand, Hand challengedHand, ChallengeState state)
    {
        ChallengerDiscordId = challengerDiscordId;
        ChallengedDiscordId = challengedDiscordId;
        Wager = wager;
        Date = date;
        ChallengerHand = challengerHand;
        ChallengedHand = challengedHand;
        State = state;
    }

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("ChallengerDiscordId")]
    [JsonPropertyName("ChallengerDiscordId")]
    public string ChallengerDiscordId { get; set; }
    
    [BsonElement("ChallengedDiscordId")]
    [JsonPropertyName("ChallengedDiscordId")]
    public string ChallengedDiscordId { get; set; }
    
    [BsonElement("Wager")]
    [JsonPropertyName("Wager")]
    public int Wager { get; set; }
    
    [BsonElement("Date")]
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [BsonElement("ChallengerHand")]
    [JsonPropertyName("ChallengerHand")]
    public Hand ChallengerHand { get; set; }

    [BsonElement("ChallengedHand")]
    [JsonPropertyName("ChallengedHand")]
    public Hand ChallengedHand { get; set; }
    
    [BsonElement("State")]
    [JsonPropertyName("State")]
    public ChallengeState State { get; set; }
}