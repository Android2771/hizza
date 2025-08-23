using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HizzaCoinBackend.Models;

public class Transaction
{
    public Transaction()
    {
    }

    public Transaction(string senderDiscordId, string receiverDiscordId, long amount, DateTime date, TransactionType transactionType)
    {
        SenderDiscordId = senderDiscordId;
        ReceiverDiscordId = receiverDiscordId;
        Amount = amount;
        Date = date;
        TransactionType = transactionType;
    }

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
    public long Amount { get; set; }
    
    [BsonElement("Date")]
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }
    
    [BsonElement("TransactionType")]
    [JsonPropertyName("TransactionType")]
    public TransactionType TransactionType { get; set; }
    
}