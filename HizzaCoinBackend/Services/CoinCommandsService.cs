using HizzaCoinBackend.Models;
using HizzaCoinBackend.Models.DTOs;
using System;
using System.Security.Cryptography;

namespace HizzaCoinBackend.Services;

public class CoinCommandsService
{
    private readonly AccountsService _accountsService;
    private readonly TransactionsService _transactionsService;
    private readonly RewardsService _rewardsService;
    private readonly ChallengesService _challengesService;
    private readonly RouletteService _rouletteService;

    public CoinCommandsService(AccountsService accountsService, TransactionsService transactionsService,
        RewardsService rewardsService, ChallengesService challengesService, RouletteService rouletteService)
    {
        _accountsService = accountsService;
        _transactionsService = transactionsService;
        _rewardsService = rewardsService;
        _challengesService = challengesService;
        _rouletteService = rouletteService;
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
        if (account == null || account.LastClaimDate == DateTime.UtcNow.Date)
        {
            return new CoinClaimResponse(
                discordId,
                0,
                account.Streak,
                0,
                new Reward(),
                new Reward(),
                0,
                false
            );
        }

        //Calculate total base claim with streak
        var baseClaim = GetBaseClaim();
        var nextReward = await _rewardsService.GetAsyncNextReward(account.Streak);
        account.Streak = account.LastClaimDate == DateTime.UtcNow.Date.AddDays(-1) || account.LastClaimDate == DateTime.UtcNow.Date.AddDays(-2) ? account.Streak + 1 : 0;
        var totalClaim = baseClaim + Math.Min(account.Streak, 10);

        //Add reward
        var claimedReward = new Reward();
        if (nextReward != null && nextReward.Streak == account.Streak)
        {
            claimedReward = nextReward;
            nextReward = await _rewardsService.GetAsyncNextReward(account.Streak);
            totalClaim += nextReward.RewardedAmount;
        }

        //Add Multiplier
        Random random = new Random();
        var addMultiplier = random.Next(0, 100) < 13;
        var multiplier = addMultiplier ? GetMultiplier() : 1;
        totalClaim = (int)(totalClaim * multiplier);

        account.LastClaimDate = DateTime.UtcNow.Date;
        account.Balance += totalClaim;

        //Add to account and create transaction
        Transaction transaction = new Transaction(
            "0",
            discordId,
            totalClaim,
            DateTime.Now,
            TransactionType.Claim
        );

        if (newAccount)
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
        );
    }

    public async Task<CoinBalanceResponse?> CoinBalance(string discordId)
    {
        var account = await _accountsService.GetAsyncByDiscordId(discordId);
        if (account == null)
            return null;
        
        return new CoinBalanceResponse(account.Balance, await GetWageredBalance(account));
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
                .Count + 1;
            double economyPercentage = (double)requesterAccount.Balance / totalHizzaCoinAmount * 100;
            economyPercentage = Math.Round(economyPercentage, 2, MidpointRounding.AwayFromZero);

            return new CoinEconomyResponse(totalHizzaCoinAmount, totalHizzaCoinAccounts, leaderboardPlace,
                economyPercentage);
        }

        return null;
    }

    public async Task<bool> CoinGive(string senderDiscordId, string receiverDiscordId, int amountToSend, bool isBet)
    {
        if (amountToSend <= 0)
            return false;

        var senderAccount = await _accountsService.GetAsyncByDiscordId(senderDiscordId);
        var receiverAccount = await _accountsService.GetAsyncByDiscordId(receiverDiscordId);
        if (senderAccount == null || receiverAccount == null)
            return false;

        var effectiveBalance = senderAccount.DiscordId == "0" ? amountToSend : senderAccount.Balance - await GetWageredBalance(senderAccount);
        if (senderAccount.Id == null || receiverAccount?.Id == null || effectiveBalance < amountToSend ||
            receiverAccount.Id == senderAccount.Id)
            return false;

        senderAccount.Balance -= senderAccount.DiscordId == "0" ? 0 : amountToSend;
        receiverAccount.Balance += receiverAccount.DiscordId == "0" ? 0 : amountToSend;

        var transaction = new Transaction(
            senderDiscordId,
            receiverDiscordId,
            amountToSend,
            DateTime.Now,
            isBet ? TransactionType.Roulette : TransactionType.Give
        );

        await _accountsService.UpdateAsync(senderAccount.Id, senderAccount);
        await _accountsService.UpdateAsync(receiverAccount.Id, receiverAccount);

        await _transactionsService.CreateAsync(transaction);

        return true;
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
            ChallengeState.InProgress);

        await _challengesService.CreateAsync(challenge);

        Task.Factory.StartNew(() => { Task.Delay(1800000).ContinueWith(t => CancelChallenge(challenge.Id)); });

        return challenge;
    }

    public async Task<Challenge?> RespondChallenge(string discordId, string wagerId, Hand hand)
    {
        var challenge = await _challengesService.GetAsync(wagerId);
        if (challenge is not {State: ChallengeState.InProgress})
            return null;

        if (challenge.State == ChallengeState.InProgress)
        {
            if (challenge.ChallengerDiscordId == discordId && challenge.ChallengerHand == Hand.NotSelected)
            {
                //If challenger responds, simply record hand
                challenge.ChallengerHand = hand;
            }
            else if (challenge.ChallengedDiscordId == discordId && challenge.ChallengedHand == Hand.NotSelected)
            {
                //If challenged responds, take bet amount
                challenge.ChallengedHand = hand;
                var challengedAccount = await _accountsService.GetAsyncByDiscordId(discordId);
                if (challengedAccount == null)
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

            Transaction transaction = new Transaction();
            switch (challengeState)
            {
                case ChallengeState.PlayerOneWin:
                    challengerAccount.Balance += challenge.Wager;
                    challengedAccount.Balance -= challenge.Wager;

                    transaction = new Transaction(
                        challenge.ChallengedDiscordId,
                        challenge.ChallengerDiscordId,
                        challenge.Wager,
                        DateTime.Now,
                        TransactionType.Challenge
                    );
                    break;
                case ChallengeState.PlayerTwoWin:
                    challengerAccount.Balance -= challenge.Wager;
                    challengedAccount.Balance += challenge.Wager;

                    transaction = new Transaction(
                        challenge.ChallengerDiscordId,
                        challenge.ChallengedDiscordId,
                        challenge.Wager,
                        DateTime.Now,
                        TransactionType.Challenge
                    );
                    break;
            }

            challenge.State = challengeState;

            await _accountsService.UpdateAsync(challengerAccount.Id, challengerAccount);
            await _accountsService.UpdateAsync(challengedAccount.Id, challengedAccount);

            await _transactionsService.CreateAsync(transaction);
        }

        await _challengesService.UpdateAsync(wagerId, challenge);
        return challenge;
    }

    public async Task<bool> CancelChallenge(string challengeId)
    {
        var challenge = await _challengesService.GetAsync(challengeId);
        if (challenge is not { State: ChallengeState.InProgress })
            return false;
        
        challenge.State = ChallengeState.Expired;
        await _challengesService.UpdateAsync(challenge.Id, challenge);
        return true;
    }

    public async Task<RouletteResponse?> RouletteNumber(string discordId, int numberBet, int bet)
    {
        var rouletteNumber = RandomNumberGenerator.GetInt32(0, 37);
        var spoils = bet * 35;
        
        if(numberBet is < 0 or > 36 || !await TakeBet(discordId, bet))
            return new RouletteResponse(0, 0, 0);

        if (numberBet == rouletteNumber)
        {
            if (await PayOutSpoils(discordId, spoils))
            {
                await _rouletteService.CreateAsync(new Roulette(bet, spoils, numberBet, rouletteNumber, 1));
                return new RouletteResponse(rouletteNumber, bet, spoils);
            }

            await _rouletteService.CreateAsync(new Roulette(bet, 0, numberBet, rouletteNumber, 1));
            return new RouletteResponse(0, 0, 0);
        }

        return new RouletteResponse(rouletteNumber, bet, 0);
    }

    public async Task<RouletteResponse?> RouletteTwelve(string discordId, int twelveBet, int bet)
    {
        var rouletteNumber = RandomNumberGenerator.GetInt32(0, 37);
        var spoils = bet * 3;
        
        if (twelveBet is < 1 or > 3 || !await TakeBet(discordId, bet))
            return new RouletteResponse(0, 0, 0);

        if ((twelveBet == 1 && rouletteNumber is >= 1 and <= 12) ||
                (twelveBet == 2 && rouletteNumber is >= 13 and <= 24) ||
                (twelveBet == 3 && rouletteNumber is >= 25 and <= 36))
        {
            if (await PayOutSpoils(discordId, bet * 3))
            {
                await _rouletteService.CreateAsync(new Roulette(bet, spoils, twelveBet, rouletteNumber, 2));
                return new RouletteResponse(rouletteNumber, bet, spoils);
            }

            await _rouletteService.CreateAsync(new Roulette(bet, 0, twelveBet, rouletteNumber, 2));
            return new RouletteResponse(0, 0, 0);
        }

        return new RouletteResponse(rouletteNumber, bet, 0);
    }

    public async Task<RouletteResponse?> RouletteColour(string discordId, bool isColourRedBet, int bet)
    {
        var rouletteNumber = RandomNumberGenerator.GetInt32(0, 37);
        var spoils = bet * 2;

        if (!await TakeBet(discordId, bet))
            return new RouletteResponse(0, 0, 0);
        
        int[] redColours = [1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 13, 25, 27, 30, 32, 34, 36];
        if (rouletteNumber != 0 && (isColourRedBet && redColours.Contains(rouletteNumber) || !isColourRedBet && !redColours.Contains(rouletteNumber)))
        {
            if (await PayOutSpoils(discordId, bet * 2))
            {
                
                return new RouletteResponse(rouletteNumber, bet, spoils);
            }

            return new RouletteResponse(0, 0, 0);
        }

        return new RouletteResponse(rouletteNumber, bet, 0);
    }


    private async Task<bool> TakeBet(string discordId, int bet) =>
        await CoinGive(discordId, "0", bet, true);

    private async Task<bool> PayOutSpoils(string discordId, int spoils) =>
        await CoinGive("0", discordId, spoils, true);

    private int GetBaseClaim()
    {
        DateTime date = DateTime.Now;
        int seed = date.Day + date.Month;

        if (seed % 3 == 0 || seed % 5 == 0)
            return 5;
        if (seed % 17 == 0)
            return 10;
        if (seed % 4 == 0 || seed % 7 == 0)
            return 7;
        if (seed % 2 == 1)
            return 4;
        
        return 2;
    }

    private double GetMultiplier()
    {
        Random rand = new Random();
        double randomValue = 1.1 + rand.NextDouble() * (5.0 - 1.1);
        return Math.Round(randomValue, 2);
    }

    private async Task<int> GetEffectiveBalance(Account account) =>
        account.Balance - await GetWageredBalance(account);
    
    private async Task<int> GetWageredBalance(Account account)
    {
        var challenges = await _challengesService.GetAsync();
        var totalBetAmount = challenges
            .Where(o => account.DiscordId == o.ChallengerDiscordId || account.DiscordId == o.ChallengedDiscordId)
            .Where(o => o.State == ChallengeState.InProgress)
            .Sum(o => o.Wager);

        return totalBetAmount;
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
