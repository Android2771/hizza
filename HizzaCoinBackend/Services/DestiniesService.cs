using HizzaCoinBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HizzaCoinBackend.Services;

public class DestiniesService
{
    private readonly IMongoCollection<Destiny> _destiniesCollection;

    public DestiniesService(IMongoDatabase database)
    {
        _destiniesCollection = database.GetCollection<Destiny>("Destinies");
    }

    public async Task<List<Destiny>> GetAsync() =>
        await _destiniesCollection.Find(destiny => true).ToListAsync();
    
    public async Task<Destiny?> GetAsync(string id) =>
        await _destiniesCollection.Find(destiny => destiny.Id == id).FirstOrDefaultAsync();
    
    public async Task CreateAsync(Destiny destiny) =>
        await _destiniesCollection.InsertOneAsync(destiny);
    
    public async Task UpdateAsync(string id, Destiny updatedDestiny) =>
        await _destiniesCollection.ReplaceOneAsync(destiny => destiny.Id == id, updatedDestiny);
    
    public async Task RemoveAsync(string id) =>
        await _destiniesCollection.DeleteOneAsync(destiny => destiny.Id == id);
}