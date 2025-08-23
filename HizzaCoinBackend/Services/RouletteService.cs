using HizzaCoinBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HizzaCoinBackend.Services;

public class RouletteService
{
    private readonly IMongoCollection<Roulette> _rouletteCollection;

    public RouletteService(IMongoDatabase database)
    {
        _rouletteCollection = database.GetCollection<Roulette>("Roulettes");
    }

    public async Task<List<Roulette>> GetAsync() =>
        await _rouletteCollection.Find(reward => true).ToListAsync();
    
    public async Task<Roulette?> GetAsync(string id) =>
        await _rouletteCollection.Find(reward => reward.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Roulette reward) =>
        await _rouletteCollection.InsertOneAsync(reward);
    
    public async Task UpdateAsync(string id, Roulette updatedRoulette) =>
        await _rouletteCollection.ReplaceOneAsync(reward => reward.Id == id, updatedRoulette);
    
    public async Task RemoveAsync(string id) =>
        await _rouletteCollection.DeleteOneAsync(reward => reward.Id == id);
}