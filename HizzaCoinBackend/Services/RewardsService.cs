using HizzaCoinBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HizzaCoinBackend.Services;

public class RewardsService
{
    private readonly IMongoCollection<Reward> _rewardsCollection;

    public RewardsService(IMongoDatabase database)
    {
        _rewardsCollection = database.GetCollection<Reward>("Rewards");
    }

    public async Task<List<Reward>> GetAsync() =>
        await _rewardsCollection.Find(reward => true).ToListAsync();
    
    public async Task<Reward?> GetAsync(string id) =>
        await _rewardsCollection.Find(reward => reward.Id == id).FirstOrDefaultAsync();

    public async Task<Reward?> GetAsyncNextReward(long streak)
    {
        var filter = Builders<Reward>.Filter.Gt(o => o.Streak, streak);
        var sort = Builders<Reward>.Sort.Ascending(o => o.Streak);
        return await _rewardsCollection.Find(filter).Sort(sort).Limit(1).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(Reward reward) =>
        await _rewardsCollection.InsertOneAsync(reward);
    
    public async Task UpdateAsync(string id, Reward updatedReward) =>
        await _rewardsCollection.ReplaceOneAsync(reward => reward.Id == id, updatedReward);
    
    public async Task RemoveAsync(string id) =>
        await _rewardsCollection.DeleteOneAsync(reward => reward.Id == id);
}