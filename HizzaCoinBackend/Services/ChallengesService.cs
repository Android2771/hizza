using HizzaCoinBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HizzaCoinBackend.Services;

public class ChallengesService
{
    private readonly IMongoCollection<Challenge> _challengesCollection;

    public ChallengesService(IMongoDatabase database)
    {
        _challengesCollection = database.GetCollection<Challenge>("Challenges");
    }

    public async Task<List<Challenge>> GetAsync() =>
        await _challengesCollection.Find(challenge => true).ToListAsync();

    public async Task<List<Challenge>> GetAsyncByDiscordId(string discordId) =>
        await _challengesCollection.Find(challenge => challenge.ChallengerDiscordId == discordId).ToListAsync();
    
    public async Task<Challenge?> GetAsync(string id) =>
        await _challengesCollection.Find(challenge => challenge.Id == id).FirstOrDefaultAsync();
    
    public async Task CreateAsync(Challenge challenge) =>
        await _challengesCollection.InsertOneAsync(challenge);
    
    public async Task UpdateAsync(string id, Challenge updatedChallenge) =>
        await _challengesCollection.ReplaceOneAsync(challenge => challenge.Id == id, updatedChallenge);
    
    public async Task RemoveAsync(string id) =>
        await _challengesCollection.DeleteOneAsync(challenge => challenge.Id == id);
}