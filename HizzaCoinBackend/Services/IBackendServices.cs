using HizzaCoinBackend.Models;
using HizzaCoinBackend.Models.DTOs;

namespace HizzaCoinBackend.Services;

public interface IAccountsService
{
    Task<List<Account>> GetAsync();
    Task<long> GetActiveUsers();
    Task<long> GetCount();
    Task<Account?> GetAsync(string id);
    Task<Account?> GetAsyncByDiscordId(string discordId);
    Task<List<Account>> GetAsyncTopFiveBalances();
    Task CreateAsync(Account account);
    Task UpdateAsync(string id, Account updatedAccount);
    Task RemoveAsync(string id);
}

public interface IChallengesService
{
    Task<List<Challenge>> GetAsync();
    Task<long> GetCount();
    Task<List<Challenge>> GetAsyncByDiscordId(string discordId);
    Task<Challenge?> GetAsync(string id);
    Task CreateAsync(Challenge challenge);
    Task UpdateAsync(string id, Challenge updatedChallenge);
    Task CancelAllChallenges();
    Task CancelChallengesByDiscordId(string discordId);
    Task RemoveAsync(string id);
}

public interface ITransactionsService
{
    Task<List<Transaction>> GetAsync();
    Task<long> GetBetsPlaced();
    Task<long> GetBetsWon();
    Task<long> GetClaimsMade();
    Task<long> GetExchangesMade();
    Task<Transaction?> GetAsync(string id);
    Task CreateAsync(Transaction transaction);
    Task UpdateAsync(string id, Transaction updatedTransaction);
    Task RemoveAsync(string id);
}

public interface IRewardsService
{
    Task<List<Reward>> GetAsync();
    Task<Reward?> GetAsync(string id);
    Task<Reward?> GetAsyncNextReward(long streak);
    Task CreateAsync(Reward reward);
    Task UpdateAsync(string id, Reward updatedReward);
    Task RemoveAsync(string id);
}

public interface IRouletteService
{
    Task<List<Roulette>> GetAsync();
    Task<Roulette?> GetAsync(string id);
    Task CreateAsync(Roulette roulette);
    Task UpdateAsync(string id, Roulette updatedRoulette);
    Task RemoveAsync(string id);
}

public interface IStatisticsService
{
    Task<StatisticsResponse?> GetStats();
}

public interface ICoinCommandsService
{
    Task<CoinClaimResponse?> CoinClaim(string discordId);
    Task<CoinBalanceResponse?> CoinBalance(string discordId);
    Task<List<Account>> CoinLeaderboard();
    Task<CoinEconomyResponse?> CoinEconomy(string discordId);
    Task<Transaction> CoinGive(string senderDiscordId, string receiverDiscordId, long amountToSend, bool isBet);
    Task<Challenge?> InitiateChallenge(string challengerDiscordId, string challengedDiscordId, long wager);
    Task<Challenge?> RespondChallenge(string discordId, string wagerId, Hand hand);
    Task<bool> CancelChallenge(string challengeId);
    Task<RouletteResponse?> RouletteNumber(string discordId, string numberBets, long bet);
    Task<RouletteResponse?> RouletteColour(string discordId, bool isColourRedBet, long bet);
}
