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
        account.Streak = account.LastClaimDate == DateTime.UtcNow.Date.AddDays(-1) || account.LastClaimDate == DateTime.UtcNow.Date.AddDays(-2) || account.Streak <= 30 ? account.Streak + 1 : 0;
        var totalClaim = baseClaim + Math.Min(account.Streak, 30);

        //Add reward
        var claimedReward = new Reward();
        if (nextReward != null && nextReward.Streak == account.Streak)
        {
            claimedReward = nextReward;
            nextReward = await _rewardsService.GetAsyncNextReward(account.Streak);
            totalClaim += claimedReward.RewardedAmount;
        }

        //Add Multiplier
        var addMultiplier = RandomNumberGenerator.GetInt32(0, 100);
        var maxMultiplier = claimedReward.RewardedAmount > 0 ? 5 : 15;
        switch (GetDestiny())
        {
            case Destiny.Small: 
                maxMultiplier = (int)(maxMultiplier * 0.7); 
                addMultiplier = (int)(addMultiplier * 0.7); 
            break;
            case Destiny.Somewhat: 
                maxMultiplier = (int)(maxMultiplier * 0.9); 
                addMultiplier = (int)(addMultiplier * 0.9); 
            break;
            case Destiny.Very: 
                maxMultiplier = (int)(maxMultiplier * 1.1);
                addMultiplier = (int)(addMultiplier * 1.1);
            break;
            case Destiny.Insane: 
                maxMultiplier = (int)(maxMultiplier * 1.3);
                addMultiplier = (int)(addMultiplier * 1.3);
            break;
        }
        var multiplier = addMultiplier < 15 ? GetMultiplier(maxMultiplier) : 1;
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

    public async Task<Transaction> CoinGive(string senderDiscordId, string receiverDiscordId, long amountToSend, bool isBet)
    {
        if (amountToSend <= 0)
            return new Transaction();

        var senderAccount = await _accountsService.GetAsyncByDiscordId(senderDiscordId);
        var receiverAccount = await _accountsService.GetAsyncByDiscordId(receiverDiscordId);
        if (senderAccount == null || receiverAccount == null)
            return new Transaction();

        var effectiveBalance = senderAccount.DiscordId == "0" ? amountToSend : senderAccount.Balance - await GetWageredBalance(senderAccount);
        if (senderAccount.Id == null || receiverAccount?.Id == null || effectiveBalance < amountToSend ||
            receiverAccount.Id == senderAccount.Id)
            return new Transaction();

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

        return transaction;
    }

    public async Task<Challenge?> InitiateChallenge(string challengerDiscordId, string challengedDiscordId, long wager)
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

        Task.Factory.StartNew(() => { Task.Delay(TimeSpan.FromDays(7)).ContinueWith(t => CancelChallenge(challenge.Id)); });

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

    public async Task<RouletteResponse?> RouletteNumber(string discordId, string numberBets, long bet)
    {
        var numberBetStrings = numberBets.Split(",");
        List<long> numberBetInts = new List<long>();
        foreach (var numberBetString in numberBetStrings)
        {
            numberBetInts.Add(Convert.ToInt64(numberBetString));
        }
        
        var rouletteNumber = RandomNumberGenerator.GetInt32(0, 37);
        var spoils = (long)(bet * Math.Round(Math.Pow((double)numberBetInts.Count / 36, -1), 2));
        spoils = spoils < bet ? bet : spoils;
        
        //Validate numbers and bet
        foreach (long numberBet in numberBetInts)
        {
            if (numberBet is < 0 or > 36)
                return new RouletteResponse(0, 0, 0, false);
        }

        if ((await TakeBet(discordId, bet)).Id == null)
        {
            return new RouletteResponse(0, 0, 0, false);
        }
        
        foreach(long numberBet in numberBetInts)
        {
            if (numberBet == rouletteNumber)
                {
                    if ((await PayOutSpoils(discordId, spoils)).Id != null)
                    {
                        return new RouletteResponse(rouletteNumber, bet, spoils, false);
                    }

                    return new RouletteResponse(0, 0, 0, false);
                }
        }

        return new RouletteResponse(rouletteNumber, bet, 0, false);
    }
    public async Task<RouletteResponse?> RouletteColour(string discordId, bool isColourRedBet, long bet)
    {
        var rouletteNumber = RandomNumberGenerator.GetInt32(0, 10);
        var spoils = bet * 2;
        var betTransaction = await TakeBet(discordId, bet);
        var destinyIntervened = false;
        
        Roulette roulette = new Roulette(isColourRedBet ? 1 : 0, rouletteNumber, RouletteType.Colour);
        roulette.WageredTransactionId = betTransaction.Id;
        
        if(betTransaction.Id == null)
            return new RouletteResponse(0, 0, 0, destinyIntervened);

        int[] redColours = [1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 13, 25, 27, 30, 32, 34, 36];
        
        switch (GetDestiny())
        {
            case Destiny.Small:
                if ((isColourRedBet && rouletteNumber is 1 or 3 or 5) || (!isColourRedBet && rouletteNumber is 2 or 4 or 6))
                {
                    rouletteNumber++;
                    destinyIntervened = true;
                    roulette.RolledNumber = rouletteNumber;
                }
                break;
            case Destiny.Somewhat:
                if ((isColourRedBet && rouletteNumber == 1) || (!isColourRedBet && rouletteNumber == 2))
                {
                    rouletteNumber++;
                    destinyIntervened = true;
                    roulette.RolledNumber = rouletteNumber;
                }
                break;
            case Destiny.Very:
                if ((isColourRedBet && rouletteNumber == 2) || (!isColourRedBet && rouletteNumber == 1))
                {
                    rouletteNumber++;
                    destinyIntervened = true;
                    roulette.RolledNumber = rouletteNumber;
                }
                break;
            case Destiny.Insane:
                if ((isColourRedBet && rouletteNumber is 2 or 4 or 6) || (!isColourRedBet && rouletteNumber is 1 or 3 or 5))
                {
                    rouletteNumber++;
                    destinyIntervened = true;
                    roulette.RolledNumber = rouletteNumber;
                }
                break;
        }

        if (rouletteNumber != 0 && (isColourRedBet && redColours.Contains(rouletteNumber) || !isColourRedBet && !redColours.Contains(rouletteNumber)))
        {
            var rewardTransaction = await PayOutSpoils(discordId, bet * 2);
            if (rewardTransaction.Id != null)
            {
                roulette.RewardTransactionId = rewardTransaction.Id;
                await _rouletteService.CreateAsync(roulette);
                return new RouletteResponse(rouletteNumber, bet, spoils, destinyIntervened);
            }

            return new RouletteResponse(0, 0, 0, destinyIntervened);
        }

        await _rouletteService.CreateAsync(roulette);
        return new RouletteResponse(rouletteNumber, bet, 0, destinyIntervened);
    }


    private async Task<Transaction> TakeBet(string discordId, long bet) =>
        await CoinGive(discordId, "0", bet, true);

    private async Task<Transaction> PayOutSpoils(string discordId, long spoils) =>
        await CoinGive("0", discordId, spoils, true);

    private long GetBaseClaim()
    {
        return GetDestiny() switch
        {
            Destiny.Small => 15,
            Destiny.Somewhat => 30,
            Destiny.Big => 40,
            Destiny.Very => 50,
            Destiny.Insane => 70,
            _ => 2
        };
    }

    private Destiny GetDestiny()
    {
        DateTime date = DateTime.Now;
        long seed = date.Day + date.Month - 1;

        if (seed % 3 == 0 || seed % 5 == 0)
            return Destiny.Big;
        if (seed % 17 == 0)
            return Destiny.Insane;
        if (seed % 4 == 0 || seed % 7 == 0)
            return Destiny.Very;
        if (seed % 2 == 1)
            return Destiny.Somewhat;

        return Destiny.Small;
    }

    private double GetMultiplier(double max)
    {
        Random rand = new Random();
        double randomValue = 1.1 + rand.NextDouble() * (max - 1.1);
        return Math.Round(randomValue, 2);
    }

    private async Task<long> GetEffectiveBalance(Account account) =>
        account.Balance - await GetWageredBalance(account);
    
    private async Task<long> GetWageredBalance(Account account)
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
