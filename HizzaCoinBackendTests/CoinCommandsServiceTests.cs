using HizzaCoinBackend.Models;
using HizzaCoinBackend.Services;
using Moq;

namespace HizzaCoinBackendTests;

public class CoinCommandsServiceTests
{
    private readonly Mock<IAccountsService> _accounts = new();
    private readonly Mock<ITransactionsService> _transactions = new();
    private readonly Mock<IRewardsService> _rewards = new();
    private readonly Mock<IChallengesService> _challenges = new();
    private readonly Mock<IRouletteService> _roulette = new();
    private readonly FixedCoinRandomSource _randomSource = new();
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2026, 11, 12, 12, 0, 0, TimeSpan.Zero));

    public CoinCommandsServiceTests()
    {
        _challenges.Setup(x => x.GetAsync()).ReturnsAsync([]);
        _challenges.Setup(x => x.CreateAsync(It.IsAny<Challenge>())).Returns(Task.CompletedTask);
        _challenges.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Challenge>())).Returns(Task.CompletedTask);
        _transactions.Setup(x => x.CreateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        _accounts.Setup(x => x.CreateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        _accounts.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Account>())).Returns(Task.CompletedTask);
        _roulette.Setup(x => x.CreateAsync(It.IsAny<Roulette>())).Returns(Task.CompletedTask);
    }

    private CoinCommandsService CreateService(IDestinyProvider? destinyProvider = null) =>
        new(_accounts.Object, _transactions.Object, _rewards.Object, _challenges.Object, _roulette.Object,
            _randomSource, _timeProvider, destinyProvider ?? new DestinyProvider(_timeProvider));

    private DateTime Today => _timeProvider.GetUtcNow().UtcDateTime.Date;

    [Fact]
    public async Task CoinClaim_returns_unclaimed_when_account_already_claimed_today()
    {
        var account = NewAccount("user", 100, Today, streak: 12);
        SetupAccounts(account);

        var response = await CreateService().CoinClaim("user");

        Assert.NotNull(response);
        Assert.False(response.Claimed);
        Assert.Equal(0, response.TotalClaim);
        Assert.Equal(12, response.Streak);
        _accounts.Verify(x => x.CreateAsync(It.IsAny<Account>()), Times.Never);
        _accounts.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Account>()), Times.Never);
        _transactions.Verify(x => x.CreateAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task CoinClaim_creates_first_account_and_claim_transaction()
    {
        SetupAccounts();
        _rewards.Setup(x => x.GetAsyncNextReward(It.IsAny<long>())).ReturnsAsync((Reward?)null);
        Account? createdAccount = null;
        Transaction? createdTransaction = null;
        _accounts.Setup(x => x.CreateAsync(It.IsAny<Account>()))
            .Callback<Account>(account => createdAccount = account).Returns(Task.CompletedTask);
        _transactions.Setup(x => x.CreateAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(transaction => createdTransaction = transaction).Returns(Task.CompletedTask);

        var response = await CreateService().CoinClaim("new-user");

        Assert.NotNull(response);
        Assert.True(response.Claimed);
        Assert.Equal(1, response.Streak);
        Assert.True(response.TotalClaim > 0);
        Assert.Equal(response.TotalClaim, createdAccount!.Balance);
        Assert.Equal(Today, createdAccount.LastClaimDate);
        Assert.Equal("new-user", createdAccount.DiscordId);
        Assert.Equal(TransactionType.Claim, createdTransaction!.TransactionType);
        Assert.Equal("0", createdTransaction.SenderDiscordId);
        Assert.Equal("new-user", createdTransaction.ReceiverDiscordId);
        Assert.Equal(response.TotalClaim, createdTransaction.Amount);
    }

    [Fact]
    public async Task CoinClaim_advances_existing_streak_and_applies_streak_reward()
    {
        var account = NewAccount("user", 500, Today.AddDays(-1), streak: 5);
        SetupAccounts(account);
        var reward = new Reward { Streak = 6, RewardedAmount = 20 };
        _rewards.Setup(x => x.GetAsyncNextReward(It.IsAny<long>()))
            .Returns((long streak) => Task.FromResult<Reward?>(streak == 5 ? reward : null));

        var response = await CreateService().CoinClaim("user");

        Assert.NotNull(response);
        Assert.True(response.Claimed);
        Assert.Equal(6, response.Streak);
        Assert.Equal(5000, response.ClaimedReward.RewardedAmount);
        Assert.Equal(account.Balance, 500 + response.TotalClaim);
        Assert.Equal(Today, account.LastClaimDate);
        _accounts.Verify(x => x.UpdateAsync("user-id", account), Times.Once);
    }

    [Fact]
    public async Task CoinClaim_resets_a_streak_after_the_grace_period()
    {
        var account = NewAccount("user", 0, Today.AddDays(-3), streak: 31);
        SetupAccounts(account);
        _rewards.Setup(x => x.GetAsyncNextReward(It.IsAny<long>())).ReturnsAsync((Reward?)null);

        var response = await CreateService().CoinClaim("user");

        Assert.NotNull(response);
        Assert.Equal(0, response.Streak);
    }

    [Theory]
    [InlineData(12, 11, 17500)]
    [InlineData(4, 11, 33750)]
    [InlineData(7, 2, 33750)]
    [InlineData(17, 11, 43750)]
    [InlineData(3, 11, 55000)]
    [InlineData(7, 11, 77500)]
    public async Task CoinClaim_uses_the_daily_destiny_base_claim(int day, int month, long expectedBaseClaim)
    {
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, month, day, 12, 0, 0, TimeSpan.Zero));
        _randomSource.QueueIntegers(100, 0, 99);
        SetupAccounts();
        _rewards.Setup(x => x.GetAsyncNextReward(It.IsAny<long>())).ReturnsAsync((Reward?)null);

        var response = await CreateService().CoinClaim("user");

        Assert.NotNull(response);
        Assert.Equal(expectedBaseClaim, response.BaseClaim);
        Assert.Equal(1, response.Multiplier);
    }

    [Fact]
    public async Task CoinClaim_uses_the_multiplier_on_a_streak_anniversary()
    {
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 11, 12, 12, 0, 0, TimeSpan.Zero));
        _randomSource.QueueIntegers(100, 1, 99);
        _randomSource.QueueDoubles(0.5);
        var account = NewAccount("user", 100, Today.AddDays(-1), streak: 364);
        SetupAccounts(account);
        _rewards.Setup(x => x.GetAsyncNextReward(It.IsAny<long>())).ReturnsAsync((Reward?)null);

        var response = await CreateService().CoinClaim("user");

        Assert.NotNull(response);
        Assert.Equal(365, response.Streak);
        Assert.InRange(response.Multiplier, 1.1, 21);
        Assert.Equal(100 + response.TotalClaim, account.Balance);
    }

    [Fact]
    public async Task CoinClaim_does_not_apply_a_reward_for_a_later_streak()
    {
        SetupAccounts();
        var reward = new Reward { Streak = 5, RewardedAmount = 100 };
        _rewards.Setup(x => x.GetAsyncNextReward(It.IsAny<long>())).ReturnsAsync(reward);
        _randomSource.QueueIntegers(100, 0, 99);

        var response = await CreateService().CoinClaim("user");

        Assert.NotNull(response);
        Assert.Equal(0, response.ClaimedReward.RewardedAmount);
        Assert.Equal(5, response.NextReward.Streak);
    }

    [Fact]
    public async Task CoinClaim_uses_fallback_claim_for_an_unrecognized_destiny()
    {
        _randomSource.QueueIntegers(100, 0, 99);
        SetupAccounts();
        _rewards.Setup(x => x.GetAsyncNextReward(It.IsAny<long>())).ReturnsAsync((Reward?)null);

        var response = await CreateService(new FixedDestinyProvider((Destiny)99)).CoinClaim("user");

        Assert.NotNull(response);
        Assert.Equal(2500, response.BaseClaim);
    }

    [Fact]
    public async Task CoinBalance_returns_null_for_unknown_account_and_includes_active_wagers()
    {
        var account = NewAccount("user", 100, DateTime.MinValue);
        SetupAccounts(account);
        _challenges.Setup(x => x.GetAsync()).ReturnsAsync([
            NewChallenge("user", "other", 15, ChallengeState.InProgress),
            NewChallenge("user", "other", 25, ChallengeState.Expired),
            NewChallenge("another", "user", 10, ChallengeState.InProgress)
        ]);

        Assert.Null(await CreateService().CoinBalance("unknown"));
        var result = await CreateService().CoinBalance("user");

        Assert.NotNull(result);
        Assert.Equal(100, result.Balance);
        Assert.Equal(25, result.WageredBalance);
    }

    [Fact]
    public async Task CoinLeaderboard_returns_top_accounts_from_account_service()
    {
        List<Account> expected = [NewAccount("one", 20, DateTime.MinValue)];
        _accounts.Setup(x => x.GetAsyncTopFiveBalances()).ReturnsAsync(expected);

        Assert.Same(expected, await CreateService().CoinLeaderboard());
    }

    [Fact]
    public async Task CoinEconomy_returns_null_for_unknown_account_and_calculates_share_and_rank()
    {
        var requester = NewAccount("user", 25, DateTime.MinValue);
        SetupAccounts(requester);
        _accounts.Setup(x => x.GetAsync()).ReturnsAsync([
            NewAccount("richest", 75, DateTime.MinValue),
            requester,
            NewAccount("poorest", 0, DateTime.MinValue)
        ]);

        var service = CreateService();
        Assert.Null(await service.CoinEconomy("unknown"));
        var result = await service.CoinEconomy("user");

        Assert.NotNull(result);
        Assert.Equal(100, result.TotalHizzaCoinAmount);
        Assert.Equal(3, result.TotalHizzaCoinAccounts);
        Assert.Equal(2, result.LeaderboardPlace);
        Assert.Equal(25, result.PercentageEconomy);
    }

    [Fact]
    public async Task CoinGive_rejects_nonpositive_amount_missing_accounts_insufficient_balance_and_self_transfer()
    {
        var service = CreateService();
        Assert.Null((await service.CoinGive("user", "other", 0, false)).Id);

        var sender = NewAccount("user", 10, DateTime.MinValue);
        var receiver = NewAccount("other", 0, DateTime.MinValue);
        SetupAccounts(sender, receiver);
        Assert.Null((await service.CoinGive("missing", "other", 1, false)).Id);
        Assert.Null((await service.CoinGive("user", "other", 11, false)).Id);
        Assert.Null((await service.CoinGive("user", "user", 1, false)).Id);

        sender.Id = null;
        Assert.Null((await service.CoinGive("user", "other", 1, false)).Id);

        sender.Id = "user-id";
        receiver.Id = null;
        Assert.Null((await service.CoinGive("user", "other", 1, false)).Id);
        _transactions.Verify(x => x.CreateAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task CoinGive_cannot_spend_coins_reserved_by_an_active_challenge()
    {
        var sender = NewAccount("user", 20, DateTime.MinValue);
        var receiver = NewAccount("other", 0, DateTime.MinValue);
        SetupAccounts(sender, receiver);
        _challenges.Setup(x => x.GetAsync()).ReturnsAsync([
            NewChallenge("user", "other", 15, ChallengeState.InProgress)
        ]);

        var result = await CreateService().CoinGive("user", "other", 6, false);

        Assert.Null(result.Id);
        Assert.Equal(20, sender.Balance);
        Assert.Equal(0, receiver.Balance);
    }

    [Theory]
    [InlineData(false, TransactionType.Give)]
    [InlineData(true, TransactionType.Roulette)]
    public async Task CoinGive_transfers_coins_and_records_transaction(bool isBet, TransactionType expectedType)
    {
        var sender = NewAccount("user", 100, DateTime.MinValue);
        var receiver = NewAccount("other", 10, DateTime.MinValue);
        SetupAccounts(sender, receiver);
        Transaction? savedTransaction = null;
        _transactions.Setup(x => x.CreateAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(transaction => savedTransaction = transaction).Returns(Task.CompletedTask);

        var result = await CreateService().CoinGive("user", "other", 25, isBet);

        Assert.Equal(75, sender.Balance);
        Assert.Equal(35, receiver.Balance);
        Assert.Equal(expectedType, result.TransactionType);
        Assert.Equal(25, savedTransaction!.Amount);
        Assert.Equal("user", savedTransaction.SenderDiscordId);
        Assert.Equal("other", savedTransaction.ReceiverDiscordId);
        _accounts.Verify(x => x.UpdateAsync("user-id", sender), Times.Once);
        _accounts.Verify(x => x.UpdateAsync("other-id", receiver), Times.Once);
    }

    [Fact]
    public async Task CoinGive_treats_system_account_as_unlimited_source_or_sink()
    {
        var user = NewAccount("user", 5, DateTime.MinValue);
        var system = NewAccount("0", 0, DateTime.MinValue);
        SetupAccounts(user, system);

        await CreateService().CoinGive("0", "user", 50, true);
        Assert.Equal(55, user.Balance);
        Assert.Equal(0, system.Balance);

        await CreateService().CoinGive("user", "0", 20, true);
        Assert.Equal(35, user.Balance);
        Assert.Equal(0, system.Balance);
    }

    [Fact]
    public async Task InitiateChallenge_rejects_negative_wager_missing_accounts_and_same_player()
    {
        var service = CreateService();
        Assert.Null(await service.InitiateChallenge("one", "two", -1));

        var one = NewAccount("one", 100, DateTime.MinValue);
        var two = NewAccount("two", 100, DateTime.MinValue);
        SetupAccounts(one, two);
        Assert.Null(await service.InitiateChallenge("missing", "two", 1));
        Assert.Null(await service.InitiateChallenge("one", "missing", 1));
        Assert.Null(await service.InitiateChallenge("one", "one", 1));

        two.Balance = 0;
        Assert.Null(await service.InitiateChallenge("one", "two", 1));
    }

    [Fact]
    public async Task InitiateChallenge_creates_in_progress_challenge_for_valid_players()
    {
        var one = NewAccount("one", 100, DateTime.MinValue);
        var two = NewAccount("two", 100, DateTime.MinValue);
        SetupAccounts(one, two);

        var result = await CreateService().InitiateChallenge("one", "two", 25);

        Assert.NotNull(result);
        Assert.Equal("one", result.ChallengerDiscordId);
        Assert.Equal("two", result.ChallengedDiscordId);
        Assert.Equal(25, result.Wager);
        Assert.Equal(ChallengeState.InProgress, result.State);
        _challenges.Verify(x => x.CreateAsync(result), Times.Once);
    }

    [Fact]
    public async Task InitiateChallenge_accounts_for_existing_reservations_and_system_balance()
    {
        var one = NewAccount("one", 20, DateTime.MinValue);
        var two = NewAccount("two", 100, DateTime.MinValue);
        var system = NewAccount("0", 0, DateTime.MinValue);
        SetupAccounts(one, two, system);
        _challenges.Setup(x => x.GetAsync()).ReturnsAsync([
            NewChallenge("one", "other", 15, ChallengeState.InProgress)
        ]);
        var service = CreateService();

        Assert.Null(await service.InitiateChallenge("one", "two", 6));
        _challenges.Setup(x => x.GetAsync()).ReturnsAsync([]);
        var systemChallenge = await service.InitiateChallenge("one", "0", 20);

        Assert.NotNull(systemChallenge);
        Assert.Equal("0", systemChallenge.ChallengedDiscordId);
    }

    [Theory]
    [InlineData(Hand.Rock, Hand.Rock, ChallengeState.Draw, 100, 100)]
    [InlineData(Hand.Rock, Hand.Paper, ChallengeState.PlayerTwoWin, 80, 120)]
    [InlineData(Hand.Rock, Hand.Scissors, ChallengeState.PlayerOneWin, 120, 80)]
    [InlineData(Hand.Paper, Hand.Rock, ChallengeState.PlayerOneWin, 120, 80)]
    [InlineData(Hand.Paper, Hand.Paper, ChallengeState.Draw, 100, 100)]
    [InlineData(Hand.Paper, Hand.Scissors, ChallengeState.PlayerTwoWin, 80, 120)]
    [InlineData(Hand.Scissors, Hand.Rock, ChallengeState.PlayerTwoWin, 80, 120)]
    [InlineData(Hand.Scissors, Hand.Paper, ChallengeState.PlayerOneWin, 120, 80)]
    [InlineData(Hand.Scissors, Hand.Scissors, ChallengeState.Draw, 100, 100)]
    public async Task RespondChallenge_resolves_game_and_updates_balances(
        Hand challengerHand,
        Hand challengedHand,
        ChallengeState expectedState,
        long expectedChallengerBalance,
        long expectedChallengedBalance)
    {
        var challenge = NewChallenge("one", "two", 20, ChallengeState.InProgress);
        challenge.ChallengerHand = challengerHand;
        var one = NewAccount("one", 100, DateTime.MinValue);
        var two = NewAccount("two", 100, DateTime.MinValue);
        SetupAccounts(one, two);
        SetupChallengeLookup(challenge);

        var result = await CreateService().RespondChallenge("two", "challenge-id", challengedHand);

        Assert.Same(challenge, result);
        Assert.Equal(expectedState, challenge.State);
        Assert.Equal(expectedChallengerBalance, one.Balance);
        Assert.Equal(expectedChallengedBalance, two.Balance);
        _accounts.Verify(x => x.UpdateAsync("one-id", one), Times.Once);
        _accounts.Verify(x => x.UpdateAsync("two-id", two), Times.Once);
        _transactions.Verify(x => x.CreateAsync(It.IsAny<Transaction>()), Times.Once);
        _challenges.Verify(x => x.UpdateAsync("challenge-id", challenge), Times.Once);
    }

    [Fact]
    public async Task RespondChallenge_stores_first_hand_without_settling()
    {
        var challenge = NewChallenge("one", "two", 10, ChallengeState.InProgress);
        SetupChallengeLookup(challenge);

        var result = await CreateService().RespondChallenge("one", "challenge-id", Hand.Paper);

        Assert.Same(challenge, result);
        Assert.Equal(Hand.Paper, challenge.ChallengerHand);
        _transactions.Verify(x => x.CreateAsync(It.IsAny<Transaction>()), Times.Never);
        _challenges.Verify(x => x.UpdateAsync("challenge-id", challenge), Times.Once);
    }

    [Fact]
    public async Task RespondChallenge_ignores_nonparticipants()
    {
        var challenge = NewChallenge("one", "two", 10, ChallengeState.InProgress);
        SetupChallengeLookup(challenge);

        var result = await CreateService().RespondChallenge("stranger", "challenge-id", Hand.Paper);

        Assert.Same(challenge, result);
        Assert.Equal(Hand.NotSelected, challenge.ChallengerHand);
        Assert.Equal(Hand.NotSelected, challenge.ChallengedHand);
        _transactions.Verify(x => x.CreateAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task RespondChallenge_returns_null_if_an_account_disappears_before_settlement()
    {
        var challenge = NewChallenge("one", "two", 10, ChallengeState.InProgress);
        challenge.ChallengerHand = Hand.Rock;
        SetupChallengeLookup(challenge);
        SetupAccounts();

        var result = await CreateService().RespondChallenge("two", "challenge-id", Hand.Scissors);

        Assert.Null(result);
        _challenges.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Challenge>()), Times.Never);
    }

    [Fact]
    public async Task RespondChallenge_returns_null_if_the_challenger_account_is_missing_at_settlement()
    {
        var challenge = NewChallenge("one", "two", 10, ChallengeState.InProgress);
        challenge.ChallengerHand = Hand.Rock;
        SetupChallengeLookup(challenge);
        SetupAccounts(NewAccount("two", 100, DateTime.MinValue));

        var result = await CreateService().RespondChallenge("two", "challenge-id", Hand.Scissors);

        Assert.Null(result);
        _challenges.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Challenge>()), Times.Never);
    }

    [Fact]
    public async Task RespondChallenge_returns_null_if_the_challenged_account_is_missing()
    {
        var challenge = NewChallenge("one", "two", 10, ChallengeState.InProgress);
        SetupChallengeLookup(challenge);
        SetupAccounts(NewAccount("one", 100, DateTime.MinValue));

        var result = await CreateService().RespondChallenge("two", "challenge-id", Hand.Paper);

        Assert.Null(result);
        _challenges.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Challenge>()), Times.Never);
    }

    [Fact]
    public async Task RespondChallenge_can_resolve_when_the_challenger_selects_second()
    {
        var challenge = NewChallenge("one", "two", 10, ChallengeState.InProgress);
        challenge.ChallengedHand = Hand.Rock;
        var one = NewAccount("one", 100, DateTime.MinValue);
        var two = NewAccount("two", 100, DateTime.MinValue);
        SetupAccounts(one, two);
        SetupChallengeLookup(challenge);

        var result = await CreateService().RespondChallenge("one", "challenge-id", Hand.Scissors);

        Assert.Same(challenge, result);
        Assert.Equal(ChallengeState.PlayerTwoWin, challenge.State);
        Assert.Equal(90, one.Balance);
        Assert.Equal(110, two.Balance);
    }

    [Fact]
    public async Task RespondChallenge_treats_an_unrecognized_hand_as_a_draw()
    {
        var challenge = NewChallenge("one", "two", 10, ChallengeState.InProgress);
        var one = NewAccount("one", 100, DateTime.MinValue);
        var two = NewAccount("two", 100, DateTime.MinValue);
        SetupAccounts(one, two);
        SetupChallengeLookup(challenge);
        var service = CreateService();

        await service.RespondChallenge("one", "challenge-id", (Hand)99);
        var result = await service.RespondChallenge("two", "challenge-id", Hand.Rock);

        Assert.Same(challenge, result);
        Assert.Equal(ChallengeState.Draw, challenge.State);
        Assert.Equal(100, one.Balance);
        Assert.Equal(100, two.Balance);
    }

    [Fact]
    public async Task RespondChallenge_returns_null_for_missing_or_finished_challenge()
    {
        var service = CreateService();
        Assert.Null(await service.RespondChallenge("one", "missing", Hand.Rock));

        var challenge = NewChallenge("one", "two", 10, ChallengeState.Expired);
        SetupChallengeLookup(challenge);
        Assert.Null(await service.RespondChallenge("one", "finished", Hand.Rock));
    }

    [Fact]
    public async Task CancelChallenge_expires_only_in_progress_challenges()
    {
        var challenge = NewChallenge("one", "two", 10, ChallengeState.InProgress);
        SetupChallengeLookup(challenge);
        var service = CreateService();

        Assert.True(await service.CancelChallenge("challenge-id"));
        Assert.Equal(ChallengeState.Expired, challenge.State);
        _challenges.Verify(x => x.UpdateAsync("challenge-id", challenge), Times.Once);

        challenge.State = ChallengeState.PlayerOneWin;
        Assert.False(await service.CancelChallenge("challenge-id"));
        _challenges.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync((Challenge?)null);
        Assert.False(await service.CancelChallenge("missing"));
    }

    [Fact]
    public async Task RouletteNumber_rejects_out_of_range_number_without_taking_bet()
    {
        var result = await CreateService().RouletteNumber("user", "37", 10);

        Assert.NotNull(result);
        Assert.Equal(0, result.Bet);
        _accounts.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public async Task RouletteNumber_wins_when_every_valid_number_is_selected()
    {
        var user = NewAccount("user", 100, DateTime.MinValue);
        var bank = NewAccount("0", 0, DateTime.MinValue);
        SetupAccounts(user, bank);
        _transactions.Setup(x => x.CreateAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(transaction => transaction.Id = "transaction-id").Returns(Task.CompletedTask);
        var allNumbers = string.Join(",", Enumerable.Range(0, 37));

        var result = await CreateService().RouletteNumber("user", allNumbers, 10);

        Assert.NotNull(result);
        Assert.InRange(result.RouletteNumber, 0, 36);
        Assert.Equal(10, result.Bet);
        Assert.True(result.Payout >= 10);
        Assert.Equal(100 - 10 + result.Payout, user.Balance);
    }

    [Fact]
    public async Task RouletteNumber_returns_a_loss_when_the_number_is_not_selected()
    {
        var user = NewAccount("user", 100, DateTime.MinValue);
        var bank = NewAccount("0", 0, DateTime.MinValue);
        SetupAccounts(user, bank);
        _randomSource.QueueIntegers(5);
        _transactions.Setup(x => x.CreateAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(transaction => transaction.Id = "transaction-id").Returns(Task.CompletedTask);

        var result = await CreateService().RouletteNumber("user", "1", 10);

        Assert.NotNull(result);
        Assert.Equal(5, result.RouletteNumber);
        Assert.Equal(10, result.Bet);
        Assert.Equal(0, result.Payout);
        Assert.Equal(90, user.Balance);
    }

    [Fact]
    public async Task RouletteNumber_returns_no_bet_when_the_bet_cannot_be_taken()
    {
        var user = NewAccount("user", 5, DateTime.MinValue);
        var bank = NewAccount("0", 0, DateTime.MinValue);
        SetupAccounts(user, bank);

        var result = await CreateService().RouletteNumber("user", "0", 10);

        Assert.NotNull(result);
        Assert.Equal(0, result.Bet);
        Assert.Equal(5, user.Balance);
    }

    [Fact]
    public async Task RouletteNumber_does_not_report_a_win_if_payout_fails()
    {
        var user = NewAccount("user", 100, DateTime.MinValue);
        var bank = NewAccount("0", 0, DateTime.MinValue);
        SetupAccounts(user, bank);
        _randomSource.QueueIntegers(5);
        var createCount = 0;
        _transactions.Setup(x => x.CreateAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(transaction =>
            {
                createCount++;
                if (createCount == 1)
                    transaction.Id = "wager-id";
            }).Returns(Task.CompletedTask);

        var result = await CreateService().RouletteNumber("user", "5", 10);

        Assert.NotNull(result);
        Assert.Equal(0, result.Bet);
        Assert.Equal(0, result.Payout);
    }

    [Theory]
    [InlineData(12, 11, 1, true, true, 0)]
    [InlineData(4, 11, 1, true, true, 0)]
    [InlineData(12, 11, 2, false, true, 0)]
    [InlineData(17, 11, 1, true, false, 20)]
    [InlineData(3, 11, 2, true, true, 20)]
    [InlineData(3, 11, 1, false, true, 20)]
    [InlineData(7, 11, 2, true, true, 20)]
    [InlineData(17, 11, 2, false, false, 20)]
    [InlineData(17, 11, 1, false, false, 0)]
    [InlineData(7, 11, 1, false, true, 20)]
    [InlineData(7, 11, 2, false, false, 20)]
    [InlineData(7, 11, 1, true, false, 20)]
    public async Task RouletteColour_applies_daily_destiny_to_spins(
        int day,
        int month,
        int rolledNumber,
        bool betRed,
        bool expectedIntervention,
        long expectedPayout)
    {
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, month, day, 12, 0, 0, TimeSpan.Zero));
        _randomSource.QueueIntegers(rolledNumber, 99);
        var user = NewAccount("user", 100, DateTime.MinValue);
        var bank = NewAccount("0", 0, DateTime.MinValue);
        SetupAccounts(user, bank);
        _transactions.Setup(x => x.CreateAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(transaction => transaction.Id = "transaction-id").Returns(Task.CompletedTask);

        var result = await CreateService().RouletteColour("user", betRed, 10);

        Assert.NotNull(result);
        Assert.Equal(expectedIntervention, result.DestinyIntervened);
        Assert.Equal(expectedPayout, result.Payout);
        _roulette.Verify(x => x.CreateAsync(It.IsAny<Roulette>()), Times.Once);
    }

    [Fact]
    public async Task RouletteColour_returns_no_win_if_the_payout_transaction_fails()
    {
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero));
        _randomSource.QueueIntegers(1, 99);
        SetupAccounts(NewAccount("user", 100, DateTime.MinValue), NewAccount("0", 0, DateTime.MinValue));
        var createCount = 0;
        _transactions.Setup(x => x.CreateAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(transaction =>
            {
                createCount++;
                if (createCount == 1)
                    transaction.Id = "wager-id";
            }).Returns(Task.CompletedTask);

        var result = await CreateService().RouletteColour("user", true, 10);

        Assert.NotNull(result);
        Assert.Equal(0, result.Bet);
        Assert.Equal(0, result.Payout);
        _roulette.Verify(x => x.CreateAsync(It.IsAny<Roulette>()), Times.Never);
    }

    [Fact]
    public async Task RouletteColour_uses_default_payout_chance_for_an_unrecognized_destiny()
    {
        _randomSource.QueueIntegers(1, 99);
        SetupAccounts(NewAccount("user", 100, DateTime.MinValue), NewAccount("0", 0, DateTime.MinValue));
        _transactions.Setup(x => x.CreateAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(transaction => transaction.Id = "transaction-id").Returns(Task.CompletedTask);

        var result = await CreateService(new FixedDestinyProvider((Destiny)99)).RouletteColour("user", true, 10);

        Assert.NotNull(result);
        Assert.Equal(20, result.Payout);
        Assert.False(result.DestinyIntervened);
    }

    [Fact]
    public async Task RouletteColour_records_a_valid_spin_when_bet_is_affordable()
    {
        var user = NewAccount("user", 100, DateTime.MinValue);
        var bank = NewAccount("0", 0, DateTime.MinValue);
        SetupAccounts(user, bank);
        _transactions.Setup(x => x.CreateAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(transaction => transaction.Id = "transaction-id").Returns(Task.CompletedTask);

        var result = await CreateService().RouletteColour("user", true, 10);

        Assert.NotNull(result);
        Assert.InRange(result.RouletteNumber, 0, 36);
        Assert.Equal(10, result.Bet);
        _roulette.Verify(x => x.CreateAsync(It.IsAny<Roulette>()), Times.Once);
    }

    [Fact]
    public async Task RouletteColour_rejects_bet_when_player_has_insufficient_balance()
    {
        var user = NewAccount("user", 5, DateTime.MinValue);
        var bank = NewAccount("0", 0, DateTime.MinValue);
        SetupAccounts(user, bank);

        var result = await CreateService().RouletteColour("user", false, 10);

        Assert.NotNull(result);
        Assert.Equal(0, result.Bet);
        _roulette.Verify(x => x.CreateAsync(It.IsAny<Roulette>()), Times.Never);
    }

    private void SetupAccounts(params Account[] accounts)
    {
        var accountByDiscordId = accounts.ToDictionary(x => x.DiscordId);
        _accounts.Setup(x => x.GetAsyncByDiscordId(It.IsAny<string>()))
            .Returns((string discordId) => Task.FromResult<Account?>(
                accountByDiscordId.TryGetValue(discordId, out var account) ? account : null));
    }

    private void SetupChallengeLookup(Challenge challenge) =>
        _challenges.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(challenge);

    private static Account NewAccount(string discordId, long balance, DateTime lastClaimDate, long streak = 0) =>
        new(discordId, balance, 0, lastClaimDate, streak) { Id = $"{discordId}-id" };

    private static Challenge NewChallenge(
        string challengerId,
        string challengedId,
        long wager,
        ChallengeState state) =>
        new(challengerId, challengedId, wager, DateTime.UtcNow, Hand.NotSelected, Hand.NotSelected, state)
        {
            Id = "challenge-id"
        };

    private sealed class FixedCoinRandomSource : ICoinRandomSource
    {
        private readonly Queue<int> _integerValues = new();
        private readonly Queue<double> _doubleValues = new();

        public int GetInt32(int fromInclusive, int toExclusive)
        {
            var value = _integerValues.Count == 0 ? fromInclusive : _integerValues.Dequeue();
            if (value < fromInclusive || value >= toExclusive)
                throw new InvalidOperationException($"Configured random value {value} is outside [{fromInclusive}, {toExclusive}).");
            return value;
        }

        public double NextDouble()
        {
            var value = _doubleValues.Count == 0 ? 0 : _doubleValues.Dequeue();
            if (value is < 0 or >= 1)
                throw new InvalidOperationException($"Configured random value {value} is outside [0, 1).");
            return value;
        }

        public void QueueIntegers(params int[] values)
        {
            foreach (var value in values)
                _integerValues.Enqueue(value);
        }

        public void QueueDoubles(params double[] values)
        {
            foreach (var value in values)
                _doubleValues.Enqueue(value);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public void SetUtcNow(DateTimeOffset value) => _utcNow = value;
    }

    private sealed class FixedDestinyProvider(Destiny destiny) : IDestinyProvider
    {
        public Destiny GetDestiny() => destiny;
    }
}
