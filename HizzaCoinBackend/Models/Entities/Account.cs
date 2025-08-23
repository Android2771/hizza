using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HizzaCoinBackend.Models;

public class Account
{
    public Account(string discordId, long balance, long reservedBalance, DateTime lastClaimDate, long streak)
    {
        DiscordId = discordId;
        Balance = balance;
        ReservedBalance = reservedBalance;
        LastClaimDate = lastClaimDate;
        Streak = streak;
    }

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("DiscordId")]
    [JsonPropertyName("DiscordId")]
    public string DiscordId { get; set; }
    
    [BsonElement("Balance")]
    [JsonPropertyName("Balance")]
    public long Balance { get; set; }
    
    [BsonElement("ReservedBalance")]
    [JsonPropertyName("ReservedBalance")]
    public long ReservedBalance { get; set; }
    
    [BsonElement("LastClaimDate")]
    [JsonPropertyName("LastClaimDate")]
    public DateTime LastClaimDate { get; set; }
    
    [BsonElement("Streak")]
    [JsonPropertyName("Streak")]
    public long Streak { get; set; }
    
}