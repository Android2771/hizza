using HizzaCoinBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HizzaCoinBackend.Services;

public class AccountsService
{
    private readonly IMongoCollection<Account> _accountsCollection;

    public AccountsService(IOptions<HizzaCoinDatabaseSettings> hizzaCoinDatabaseSettings)
    {
        var client = new MongoClient(hizzaCoinDatabaseSettings.Value.ConnectionString);
        var database = client.GetDatabase(hizzaCoinDatabaseSettings.Value.DatabaseName);
        _accountsCollection = database.GetCollection<Account>("Accounts");
    }

    public async Task<List<Account>> GetAsync() =>
        await _accountsCollection.Find(account => true).ToListAsync();
    
    public async Task<Account?> GetAsync(string id) =>
        await _accountsCollection.Find(account => account.Id == id).FirstOrDefaultAsync();
    
    public async Task CreateAsync(Account account) =>
        await _accountsCollection.InsertOneAsync(account);
    
    public async Task UpdateAsync(string id, Account updatedAccount) =>
        _accountsCollection.ReplaceOneAsync(account => account.Id == id, updatedAccount);
    
    public async Task RemoveAsync(string id) =>
        await _accountsCollection.DeleteOneAsync(account => account.Id == id);
}