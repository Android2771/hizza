using HizzaCoinBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HizzaCoinBackend.Services;

public class TransactionsService
{
    private readonly IMongoCollection<Transaction> _transactionsCollection;
    
    

    public TransactionsService(IMongoDatabase database)
    {
        _transactionsCollection = database.GetCollection<Transaction>("Transactions");
    }
    
    public async Task<List<Transaction>> GetAsync() =>
        await _transactionsCollection.Find(transaction => true).ToListAsync();
    
    public async Task<long> GetBetsPlaced() =>
        await _transactionsCollection.Find(transaction => transaction.TransactionType == TransactionType.Roulette && transaction.SenderDiscordId != "0").CountDocumentsAsync();
    
    public async Task<long> GetBetsWon() =>
        await _transactionsCollection.Find(transaction => transaction.TransactionType == TransactionType.Roulette && transaction.SenderDiscordId == "0").CountDocumentsAsync();
    
    public async Task<long> GetClaimsMade() =>
        await _transactionsCollection.Find(transaction => transaction.TransactionType == TransactionType.Claim).CountDocumentsAsync();
    
    public async Task<long> GetExchangesMade() =>
        await _transactionsCollection.Find(transaction => transaction.TransactionType == TransactionType.Give).CountDocumentsAsync();

    public async Task<Transaction?> GetAsync(string id) =>
        await _transactionsCollection.Find(transaction => transaction.Id == id).FirstOrDefaultAsync();
    
    public async Task CreateAsync(Transaction transaction) =>
        await _transactionsCollection.InsertOneAsync(transaction);
    
    public async Task UpdateAsync(string id, Transaction updatedTransaction) =>
        await _transactionsCollection.ReplaceOneAsync(transaction => transaction.Id == id, updatedTransaction);
    
    public async Task RemoveAsync(string id) =>
        await _transactionsCollection.DeleteOneAsync(transaction => transaction.Id == id);
}