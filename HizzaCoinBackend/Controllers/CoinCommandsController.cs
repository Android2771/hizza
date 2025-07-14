using HizzaCoinBackend.Models;
using HizzaCoinBackend.Models.DTOs;
using HizzaCoinBackend.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace HizzaCoinBackend.Controllers;

[ApiController]
[Route("api/coin-commands")]
public class CoinCommandsController
{
    private readonly CoinCommandsService _coinCommandsService;

    public CoinCommandsController(CoinCommandsService coinCommandsService)
    {
        _coinCommandsService = coinCommandsService;
    }

    [HttpGet("coin-claim")]
    public async Task<ActionResult<CoinClaimResponse?>> CoinClaim(string discordId) =>
        await _coinCommandsService.CoinClaim(discordId);
    
    [HttpGet("coin-balance")]
    public async Task<ActionResult<CoinBalanceResponse?>> CoinBalance(string discordId) =>
        await _coinCommandsService.CoinBalance(discordId);
    
    
    [HttpGet("coin-leaderboard")]
    public async Task<ActionResult<List<Account>>> CoinLeaderboard() =>
        await _coinCommandsService.CoinLeaderboard();

    [HttpGet("coin-economy")]
    public async Task<ActionResult<CoinEconomyResponse?>> CoinEconomy(string discordId) =>
        await _coinCommandsService.CoinEconomy(discordId);

    [HttpGet("coin-give")]
    public async Task<ActionResult<bool>> CoinGive(string senderDiscordId, string receiverDiscordId, int amountToSend) =>
        await _coinCommandsService.CoinGive(senderDiscordId, receiverDiscordId, amountToSend, false);

    [HttpGet("initiate-challenge")]
    public async Task<ActionResult<Challenge?>> InitiateChallenge(string challengerDiscordId, string challengedDiscordId, int wager) =>
        await _coinCommandsService.InitiateChallenge(challengerDiscordId, challengedDiscordId, wager);
    
    [HttpGet("respond-challenge")]
    public async Task<ActionResult<Challenge?>> RespondChallenge(string discordId, string challengeId, Hand hand) =>
        await _coinCommandsService.RespondChallenge(discordId, challengeId, hand);
    
    [HttpGet("roulette-number")]

    public async Task<ActionResult<RouletteResponse?>> RouletteNumber(string discordId, int numberBet, int balance) =>
        await _coinCommandsService.RouletteNumber(discordId, numberBet, balance);
    
    [HttpGet("roulette-twelve")]

    public async Task<ActionResult<RouletteResponse?>> RouletteTwelve(string discordId, int twelveBet, int balance) =>
        await _coinCommandsService.RouletteTwelve(discordId, twelveBet, balance);
    
    [HttpGet("roulette-colour")]

    public async Task<ActionResult<RouletteResponse?>> RouletteColour(string discordId, bool isColourRedBet, int balance) =>
        await _coinCommandsService.RouletteColour(discordId, isColourRedBet, balance);
}