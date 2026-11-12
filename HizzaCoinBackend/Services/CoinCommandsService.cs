using HizzaCoinBackend.Models;
using HizzaCoinBackend.Models.DTOs;

namespace HizzaCoinBackend.Services;

public class CoinCommandsService : ICoinCommandsService
{
    private readonly IAccountsService _accountsService;
    private readonly ITransactionsService _transactionsService;
    private readonly IRewardsService _rewardsService;
    private readonly IChallengesService _challengesService;
    private readonly IRouletteService _rouletteService;
    private readonly ICoinRandomSource _randomSource;
    private readonly TimeProvider _timeProvider;
    private readonly IDestinyProvider _destinyProvider;
    private const int CoinClaimInflationIndex = 250;

    public CoinCommandsService(IAccountsService accountsService, ITransactionsService transactionsService,
        IRewardsService rewardsService, IChallengesService challengesService, IRouletteService rouletteService,
        ICoinRandomSource randomSource, TimeProvider timeProvider, IDestinyProvider destinyProvider)
    {
        _accountsService = accountsService;
        _transactionsService = transactionsService;
        _rewardsService = rewardsService;
        _challengesService = challengesService;
        _rouletteService = rouletteService;
        _randomSource = randomSource;
        _timeProvider = timeProvider;
        _destinyProvider = destinyProvider;
    }

    public async Task<CoinClaimResponse?> CoinClaim(string discordId)
    {
        //Search for account, if not create one
        var storedAccount = await _accountsService.GetAsyncByDiscordId(discordId);
        var newAccount = storedAccount == null;
        var account = storedAccount ?? new Account(discordId, 0, 0, DateTime.MinValue, 0);

        //Check whether last claim was today
        if (account.LastClaimDate == _timeProvider.GetUtcNow().UtcDateTime.Date)
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
        var baseClaim = GetBaseClaim() * CoinClaimInflationIndex * 5;
        var nextReward = await _rewardsService.GetAsyncNextReward(account.Streak);
        var claimDate = _timeProvider.GetUtcNow().UtcDateTime.Date;
        account.Streak = account.LastClaimDate == claimDate.AddDays(-1) || account.LastClaimDate == claimDate.AddDays(-2) || account.Streak <= 30 || account.LastClaimDate == new DateTime(2026, 4, 21) ? account.Streak + 1 : 0;
        var totalClaim = baseClaim + account.Streak;

        //Add reward
        var claimedReward = new Reward();
        if (nextReward != null && nextReward.Streak == account.Streak)
        {
            claimedReward = nextReward;
            nextReward = await _rewardsService.GetAsyncNextReward(account.Streak);
            claimedReward.RewardedAmount *= CoinClaimInflationIndex;
            totalClaim += claimedReward.RewardedAmount;
        }

        //Add Multiplier
        var addMultiplier = _randomSource.GetInt32(0, 100);
        var maxMultiplier = claimedReward.RewardedAmount > 0 ? 3 : 30;
        switch (_destinyProvider.GetDestiny())
        {
            case Destiny.Small: 
                maxMultiplier = (int)(maxMultiplier * 0.7); 
                addMultiplier = (int)(addMultiplier / 0.8); 
            break;
            case Destiny.Somewhat: 
                maxMultiplier = (int)(maxMultiplier * 0.9); 
                addMultiplier = (int)(addMultiplier / 0.9); 
            break;
            case Destiny.Very: 
                maxMultiplier = (int)(maxMultiplier * 1.4);
                addMultiplier = (int)(addMultiplier / 1.4);
            break;
            case Destiny.Insane: 
                maxMultiplier = (int)(maxMultiplier * 1.6);
                addMultiplier = (int)(addMultiplier / 1.8);
            break;
        }
        var multiplier = addMultiplier < 10 || account.Streak % 365 == 0 ? GetMultiplier(maxMultiplier) : 1;
        totalClaim = (int)(totalClaim * multiplier);

        account.LastClaimDate = _timeProvider.GetUtcNow().UtcDateTime.Date;
        account.Balance += totalClaim;

        //Add to account and create transaction
        Transaction transaction = new Transaction(
            "0",
            discordId,
            totalClaim,
            _timeProvider.GetLocalNow().DateTime,
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
        if (senderAccount.Id == null || receiverAccount.Id == null || effectiveBalance < amountToSend ||
            receiverAccount.Id == senderAccount.Id)
            return new Transaction();

        senderAccount.Balance -= senderAccount.DiscordId == "0" ? 0 : amountToSend;
        receiverAccount.Balance += receiverAccount.DiscordId == "0" ? 0 : amountToSend;

        var transaction = new Transaction(
            senderDiscordId,
            receiverDiscordId,
            amountToSend,
            _timeProvider.GetLocalNow().DateTime,
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
            _timeProvider.GetLocalNow().DateTime,
            Hand.NotSelected,
            Hand.NotSelected,
            ChallengeState.InProgress);

        await _challengesService.CreateAsync(challenge);

        Task.Factory.StartNew(() => { Task.Delay(TimeSpan.FromDays(1)).ContinueWith(t => CancelChallenge(challenge.Id)); });

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
                        _timeProvider.GetLocalNow().DateTime,
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
                        _timeProvider.GetLocalNow().DateTime,
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
        
        var rouletteNumber = _randomSource.GetInt32(0, 37);
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
        var rouletteNumber = _randomSource.GetInt32(0, 37);
        
        var multiplePayoutChance = _destinyProvider.GetDestiny() switch
        {
            Destiny.Small => 1,
            Destiny.Somewhat => 2,
            Destiny.Big => 3,
            Destiny.Very => 4,
            Destiny.Insane => 5,
            _ => -1
        };

        var multiplePayoutAmount = multiplePayoutChance * 3;

        var spoils = _randomSource.GetInt32(0, 100) < multiplePayoutChance ? bet * multiplePayoutAmount : bet * 2;
        var betTransaction = await TakeBet(discordId, bet);
        var destinyIntervened = false;
        
        Roulette roulette = new Roulette(isColourRedBet ? 1 : 0, rouletteNumber, RouletteType.Colour);
        roulette.WageredTransactionId = betTransaction.Id;
        
        if(betTransaction.Id == null)
            return new RouletteResponse(0, 0, 0, destinyIntervened);

        int[] redColours = [1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 13, 25, 27, 30, 32, 34, 36];
        
        switch (_destinyProvider.GetDestiny())
        {
            case Destiny.Small:
            case Destiny.Somewhat:
                if ((isColourRedBet && rouletteNumber == 1) || (!isColourRedBet && rouletteNumber == 2))
                {
                    rouletteNumber++;
                    destinyIntervened = true;
                    roulette.RolledNumber = rouletteNumber;
                }
                break;
            case Destiny.Very: 
            case Destiny.Insane:
                if ((isColourRedBet && rouletteNumber == 2) || (!isColourRedBet && rouletteNumber == 1))
                {
                    rouletteNumber++;
                    destinyIntervened = true;
                    roulette.RolledNumber = rouletteNumber;
                }
                break;
        }

        if (rouletteNumber != 0 && (isColourRedBet && redColours.Contains(rouletteNumber) || !isColourRedBet && !redColours.Contains(rouletteNumber)))
        {
            var rewardTransaction = await PayOutSpoils(discordId, spoils);
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
        var baseClaim = _destinyProvider.GetDestiny() switch
        {
            Destiny.Small => 15L,
            Destiny.Somewhat => 30L,
            Destiny.Big => 40L,
            Destiny.Very => 50L,
            Destiny.Insane => 70L,
            _ => 2
        };

        var variation = _randomSource.GetInt32(1, 401) / 100.0;
        var increase = _randomSource.GetInt32(0, 2) == 1;
        if (increase)
        {
            baseClaim += (long)(baseClaim * variation);
        }
        else
        {
            baseClaim -= (long)(baseClaim * variation) / 8;
        }

        return baseClaim;
    }

    private double GetMultiplier(double max)
    {
        double randomValue = 1.1 + _randomSource.NextDouble() * (max - 1.1);
        return Math.Round(randomValue, 2);
    }

    private async Task<long> GetEffectiveBalance(Account account) =>
        account.DiscordId == "0" ? 99999999999 : account.Balance - await GetWageredBalance(account);
    
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
