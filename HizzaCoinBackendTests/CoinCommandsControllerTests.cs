using HizzaCoinBackend.Controllers;
using HizzaCoinBackend.Models;
using HizzaCoinBackend.Models.DTOs;
using HizzaCoinBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HizzaCoinBackendTests;

public class CoinCommandsControllerTests
{
    private readonly Mock<ICoinCommandsService> _service = new();
    private readonly CoinCommandsController _controller;

    public CoinCommandsControllerTests()
    {
        _controller = new CoinCommandsController(_service.Object);
    }

    [Fact]
    public async Task Read_and_game_endpoints_return_service_results()
    {
        var claim = new CoinClaimResponse("user", 100, 1, 1, new Reward(), new Reward(), 100, true);
        var balance = new CoinBalanceResponse(200, 30);
        var leaderboard = new List<Account> { new("user", 200, 0, DateTime.MinValue, 1) };
        var economy = new CoinEconomyResponse(200, 1, 1, 100);
        var challenge = NewChallenge();
        var roulette = new RouletteResponse(5, 10, 20, false);
        _service.Setup(x => x.CoinClaim("user")).ReturnsAsync(claim);
        _service.Setup(x => x.CoinBalance("user")).ReturnsAsync(balance);
        _service.Setup(x => x.CoinLeaderboard()).ReturnsAsync(leaderboard);
        _service.Setup(x => x.CoinEconomy("user")).ReturnsAsync(economy);
        _service.Setup(x => x.CancelChallenge("challenge-id")).ReturnsAsync(true);
        _service.Setup(x => x.RouletteNumber("user", "5", 10)).ReturnsAsync(roulette);
        _service.Setup(x => x.RouletteColour("user", true, 10)).ReturnsAsync(roulette);

        Assert.Same(claim, (await _controller.CoinClaim("user")).Value);
        Assert.Same(balance, (await _controller.CoinBalance("user")).Value);
        Assert.Same(leaderboard, (await _controller.CoinLeaderboard()).Value);
        Assert.Same(economy, (await _controller.CoinEconomy("user")).Value);
        Assert.True((await _controller.CancelChallenge("challenge-id")).Value);
        Assert.Same(roulette, (await _controller.RouletteNumber("user", "5", 10)).Value);
        Assert.Same(roulette, (await _controller.RouletteColour("user", true, 10)).Value);
    }

    [Fact]
    public async Task Mutating_endpoints_return_success_or_bad_request_based_on_service_result()
    {
        var challenge = NewChallenge();
        var transaction = new Transaction("user", "other", 10, DateTime.UtcNow, TransactionType.Give)
        {
            Id = "transaction-id"
        };
        _service.SetupSequence(x => x.CoinGive("user", "other", 10, false))
            .ReturnsAsync(transaction)
            .ReturnsAsync(new Transaction());
        _service.SetupSequence(x => x.InitiateChallenge("user", "other", 10))
            .ReturnsAsync(challenge)
            .ReturnsAsync((Challenge?)null);
        _service.SetupSequence(x => x.RespondChallenge("user", "challenge-id", Hand.Rock))
            .ReturnsAsync(challenge)
            .ReturnsAsync((Challenge?)null);

        Assert.IsType<OkObjectResult>((await _controller.CoinGive("user", "other", 10)).Result);
        Assert.IsType<BadRequestResult>((await _controller.CoinGive("user", "other", 10)).Result);
        Assert.IsType<OkObjectResult>((await _controller.InitiateChallenge("user", "other", 10)).Result);
        Assert.IsType<BadRequestResult>((await _controller.InitiateChallenge("user", "other", 10)).Result);
        Assert.IsType<OkObjectResult>((await _controller.RespondChallenge("user", "challenge-id", Hand.Rock)).Result);
        Assert.IsType<BadRequestResult>((await _controller.RespondChallenge("user", "challenge-id", Hand.Rock)).Result);
    }

    private static Challenge NewChallenge() =>
        new("user", "other", 10, DateTime.UtcNow, Hand.NotSelected, Hand.NotSelected, ChallengeState.InProgress)
        {
            Id = "challenge-id"
        };
}
