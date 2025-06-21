using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HizzaCoinBackend.Models;

public class Transaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("SenderDiscordId")]
    [JsonPropertyName("SenderDiscordId")]
    public string SenderDiscordId { get; set; }
    
    [BsonElement("ReceiverDiscordId")]
    [JsonPropertyName("ReceiverDiscordId")]
    public string ReceiverDiscordId { get; set; }
    
    [BsonElement("Amount")]
    [JsonPropertyName("Amount")]
    public int Amount { get; set; }
    
    [BsonElement("Date")]
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }
    
    [BsonElement("TransactionType")]
    [JsonPropertyName("TransactionType")]
    public TransactionType TransactionType { get; set; }
    
}