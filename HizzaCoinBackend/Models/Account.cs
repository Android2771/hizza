using System.Runtime.InteropServices.JavaScript;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HizzaCoinBackend.Models;

public class Account
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string DiscordId { get; set; }
    
    public int Amount { get; set; }
    
    public JSType.Date Date { get; set; }
    
    public int Streak { get; set; }
    
}