using System.Runtime.InteropServices.JavaScript;
using HizzaCoinBackend.Models;
using HizzaCoinBackend.Models.DTOs;
using MongoDB.Driver;

namespace HizzaCoinBackend.Services;

public class CoinCommandsService
{
    private readonly AccountsService _accountsService;
    private readonly TransactionsService _transactionsService;
    private readonly RewardsService _rewardsService;
    private readonly ChallengesService _challengesService;

    public CoinCommandsService(AccountsService accountsService, TransactionsService transactionsService, RewardsService rewardsService, ChallengesService challengesService)
    {
        _accountsService = accountsService;
        _transactionsService = transactionsService;
        _rewardsService = rewardsService;
        _challengesService = challengesService;
    }

    public async Task<CoinClaimResponse?> CoinClaim(string discordId)
    {
        //Search for account, if not create one
        var account = await _accountsService.GetAsyncByDiscordId(discordId);
        var newAccount = account == null;
        if (newAccount)
        {
            account = new Account(discordId, 0, 0, DateTime.MinValue, 0);
        }
        
        //Check whether last claim was today
        // if (account == null || account.LastClaimDate == DateTime.UtcNow.Date)
        // {
        //     return new CoinClaimResponse(
        //         discordId,
        //         0,
        //         account.Streak,
        //         0,
        //         new Reward(),
        //         new Reward(),
        //         0,
        //         false
        //     );
        // }
        
        //TODO: Uncomment above as it is for testing
        
        //Calculate total base claim with streak
        var baseClaim = GetBaseClaim();
        // account.Streak = account.LastClaimDate == DateTime.UtcNow.Date.AddDays(-1) ? account.Streak + 1 : 0;
        account.Streak++;   //TODO: Change back to above as it is for testing

        var totalClaim = baseClaim;
        
        //Add reward
        var nextReward = await _rewardsService.GetAsyncNextReward(account.Streak);
        var claimedReward = new Reward();
        if (nextReward != null && nextReward.Streak == account.Streak)
        {
            claimedReward = nextReward;
            nextReward = await _rewardsService.GetAsyncNextReward(account.Streak + 1);
            totalClaim += nextReward.RewardedAmount;
        }
        
        //Add Multiplier
        Random random = new Random();
        var addMultiplier = random.Next(0, 100) < 15;
        var multiplier = addMultiplier ? GetMultiplier() : 1;
        totalClaim = (int)(totalClaim * multiplier + Math.Min(10, account.Streak));
        
        account.LastClaimDate = DateTime.UtcNow.Date;
        account.Balance += totalClaim;

        //Add to account and create transaction
        Transaction transaction = new Transaction(
            "1076237275513487361",
            discordId,
            baseClaim,
            DateTime.Now,
            TransactionType.Claim
        );
        
        if(newAccount)
            await _accountsService.CreateAsync(account);
        else
            await _accountsService.UpdateAsync(account.Id, account);
        
        await _transactionsService.CreateAsync(transaction);
        
        //Return response to bot
        return new CoinClaimResponse(
            discordId,
            baseClaim,
            account.Streak,
            multiplier,
            claimedReward,
            nextReward ?? new Reward(),
            totalClaim,
            true
        );;
    }
    
    public async Task<CoinBalanceResponse?> CoinBalance(string discordId)
    {
        var account = await _accountsService.GetAsyncByDiscordId(discordId);
        if (account == null)
            return null;
        return new CoinBalanceResponse(account.Balance, await GetEffectiveBalance(account));
    }
    
    public async Task<List<Account>> CoinLeaderboard()
    {
        return await _accountsService.GetAsyncTopFiveBalances();
    }

    public async Task<CoinEconomyResponse?> CoinEconomy(string discordId)
    {
        var requesterAccount = await _accountsService.GetAsyncByDiscordId(discordId);
        if (requesterAccount != null)
        {
            var accounts = await _accountsService.GetAsync();
            var totalHizzaCoinAmount = accounts.Sum(o => o.Balance);
            var totalHizzaCoinAccounts = accounts.Count;
            var leaderboardPlace = accounts
                .Where(o => o.Balance > requesterAccount.Balance)
                .ToList()
                .Count;
            double economyPercentage = ((double)requesterAccount.Balance / totalHizzaCoinAmount) * 100;
            economyPercentage = Math.Round(economyPercentage, 2, MidpointRounding.AwayFromZero);
            
            return new CoinEconomyResponse(totalHizzaCoinAmount, totalHizzaCoinAccounts, leaderboardPlace, economyPercentage);
        }

        return null;
    }
    
    public async Task<CoinBalanceResponse?> CoinGive(string senderDiscordId, string receiverDiscordId, int amountToSend)
    {
        if (amountToSend <= 0)
            return null;
        
        var senderAccount = await _accountsService.GetAsyncByDiscordId(senderDiscordId);
        var receiverAccount = await _accountsService.GetAsyncByDiscordId(receiverDiscordId);
        if (senderAccount == null || receiverAccount == null)
            return null;
        
        var effectiveBalance = await GetEffectiveBalance(senderAccount);
        if (senderAccount?.Id == null || receiverAccount?.Id == null || effectiveBalance < amountToSend || receiverAccount.Id == senderAccount.Id)
            return null;
        
        senderAccount.Balance -= amountToSend;
        receiverAccount.Balance += amountToSend;
        
        await _accountsService.UpdateAsync(senderAccount.Id, senderAccount);
        await _accountsService.UpdateAsync(receiverAccount.Id, receiverAccount);
        
        return new CoinBalanceResponse(senderAccount.Balance, effectiveBalance);
    }
    
    public async Task<Challenge?> InitiateChallenge(string challengerDiscordId, string challengedDiscordId, int wager)
    {
        if (wager < 0)
            return null;
        
        var challengerAccount = await _accountsService.GetAsyncByDiscordId(challengerDiscordId);
        var challengedAccount = await _accountsService.GetAsyncByDiscordId(challengedDiscordId);
        if (challengerAccount?.Id == null
            || challengedAccount?.Id == null 
            || await GetEffectiveBalance(challengerAccount) < wager 
            || await GetEffectiveBalance(challengedAccount) < wager 
            || challengedAccount.Id == challengerAccount.Id)
            return null;

        Challenge challenge = new Challenge(challengerAccount.DiscordId,
            challengedAccount.DiscordId,
            wager,
            DateTime.Now,
            Hand.NotSelected,
            Hand.NotSelected,
            ChallengeState.Draw);

        await _challengesService.CreateAsync(challenge);
        
        Task.Factory.StartNew(()=>
        {
            Task.Delay(1800000).ContinueWith(t => HandleOldChallenge(challenge.Id));
        });

        return challenge;
    }
    
    public async Task<Challenge?> RespondChallenge(string discordId, string wagerId, Hand hand)
    {
        var challenge = await _challengesService.GetAsync(wagerId);
        if (challenge == null || challenge.State != ChallengeState.InProgress)
            return null;
        
        if (challenge.State == ChallengeState.InProgress)
        {
            if (challenge.ChallengerDiscordId == discordId)
            {
                //If challenger responds, simply record hand
                challenge.ChallengerHand = hand;
            }
            else if (challenge.ChallengedDiscordId == discordId)
            {
                //If challenged responds, take bet amount
                challenge.ChallengedHand = hand;
                var challengedAccount = await _accountsService.GetAsyncByDiscordId(discordId);
                if (challengedAccount == null || await GetEffectiveBalance(challengedAccount) < challenge.Wager)
                    return null;
            }
        }

        //Check if game can reach conclusion and reconcile accounts
        if (challenge.ChallengerHand != Hand.NotSelected && challenge.ChallengedHand != Hand.NotSelected)
        {
            var challengeState = ComputeRockPaperScissors(challenge.ChallengerHand, challenge.ChallengedHand);
            var challengerAccount = await _accountsService.GetAsyncByDiscordId(challenge.ChallengerDiscordId);
            var challengedAccount = await _accountsService.GetAsyncByDiscordId(challenge.ChallengedDiscordId);
            if (challengerAccount == null || challengedAccount == null)
                return null;
            
            switch (challengeState)
            {
                case ChallengeState.PlayerOneWin:
                    challengerAccount.Balance += challenge.Wager;
                    challengedAccount.Balance -= challenge.Wager;
                    break;
                case ChallengeState.PlayerTwoWin:
                    challengerAccount.Balance -= challenge.Wager;
                    challengedAccount.Balance += challenge.Wager;
                    break;   
            }

            challenge.State = challengeState;

            await _accountsService.GetAsyncByDiscordId(challenge.ChallengerDiscordId);
            await _accountsService.GetAsyncByDiscordId(challenge.ChallengedDiscordId);
        }
        
        await _challengesService.UpdateAsync(wagerId, challenge);
        return challenge;
    }

    private async Task<bool> HandleOldChallenge(string challengeId)
    {
        var challenge = await _challengesService.GetAsync(challengeId);
        if (challenge != null)
        {
            if (challenge.State == ChallengeState.InProgress)
            {
                challenge.State = ChallengeState.Expired;
                await _challengesService.UpdateAsync(challenge.Id, challenge);
                return true;
            }
        }

        return false;
    }

    private int GetBaseClaim()
    {
        DateTime date = DateTime.Now;
        int seed = date.Day + date.Month;

        if (seed % 3 == 0 || seed % 5 == 0) // big destiny
            return 5;
        else if (seed % 17 == 0) // insane destiny
            return 10;
        else if (seed % 4 == 0 || seed % 7 == 0) // very big destiny
            return 7;
        else if (seed % 2 == 1) // somewhat big destiny
            return 4;
        else // small destiny
            return 2;
    }

    private double GetMultiplier()
    {
        Random rand = new Random();
        double randomValue = 1.1 + rand.NextDouble() * (5.0 - 1.1);
        return Math.Round(randomValue, 2);
    }

    private async Task<int> GetEffectiveBalance(Account account)
    {
        var challenges = await _challengesService.GetAsyncByDiscordId(account.DiscordId);
        var totalBetAmount = challenges
            .Where(o => o.ChallengerDiscordId == account.DiscordId 
                        && o.State == ChallengeState.InProgress)
            .Sum(o => o.Wager);

        return account.Balance - totalBetAmount;
    }

    private ChallengeState ComputeRockPaperScissors(Hand playerOneHand, Hand playerTwoHand)
    {
        return playerOneHand switch
        {
            Hand.Rock => playerTwoHand switch
            {
                Hand.Paper => ChallengeState.PlayerTwoWin,
                Hand.Scissors => ChallengeState.PlayerOneWin,
                _ => ChallengeState.Draw
            },
            Hand.Paper => playerTwoHand switch
            {
                Hand.Rock => ChallengeState.PlayerOneWin,
                Hand.Scissors => ChallengeState.PlayerTwoWin,
                _ => ChallengeState.Draw
            },
            Hand.Scissors => playerTwoHand switch
            {
                Hand.Rock => ChallengeState.PlayerTwoWin,
                Hand.Paper => ChallengeState.PlayerOneWin,
                _ => ChallengeState.Draw
            },
            _ => ChallengeState.Draw
        };
    }
}