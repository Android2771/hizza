using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HizzaCoinBackend.Models;

public class Account
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("DiscordId")]
    [JsonPropertyName("DiscordId")]
    public string DiscordId { get; set; }
    
    [BsonElement("Amount")]
    [JsonPropertyName("Amount")]
    public int Amount { get; set; }
    
    [BsonElement("ReservedAmount")]
    [JsonPropertyName("ReservedAmount")]
    public int ReservedAmount { get; set; }
    
    [BsonElement("Date")]
    [JsonPropertyName("ClaimDate")]
    public DateTime ClaimDate { get; set; }
    
    [BsonElement("Streak")]
    [JsonPropertyName("Streak")]
    public int Streak { get; set; }
    
}