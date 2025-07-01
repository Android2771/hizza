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
    
    public async Task<Transaction?> GetAsync(string id) =>
        await _transactionsCollection.Find(transaction => transaction.Id == id).FirstOrDefaultAsync();
    
    public async Task CreateAsync(Transaction transaction) =>
        await _transactionsCollection.InsertOneAsync(transaction);
    
    public async Task UpdateAsync(string id, Transaction updatedTransaction) =>
        await _transactionsCollection.ReplaceOneAsync(transaction => transaction.Id == id, updatedTransaction);
    
    public async Task RemoveAsync(string id) =>
        await _transactionsCollection.DeleteOneAsync(transaction => transaction.Id == id);
}