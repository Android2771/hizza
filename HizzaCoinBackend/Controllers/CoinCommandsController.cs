using HizzaCoinBackend.Models;
using HizzaCoinBackend.Models.DTOs;
using HizzaCoinBackend.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace HizzaCoinBackend.Controllers;

[ApiController]
[Route("api/coin-commands")]
public class CoinCommandsController : ControllerBase
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
    public async Task<ActionResult<bool>> CoinGive(string senderDiscordId, string receiverDiscordId, long amountToSend)
    {
        var give = await _coinCommandsService.CoinGive(senderDiscordId, receiverDiscordId, amountToSend, false);
        if (give)
        {
            return Ok(give);
        }

        return BadRequest();
    }

    [HttpGet("initiate-challenge")]
    public async Task<ActionResult<Challenge>> InitiateChallenge(string challengerDiscordId, string challengedDiscordId, long wager)
    {
        var challenge = await _coinCommandsService.InitiateChallenge(challengerDiscordId, challengedDiscordId, wager);
        if (challenge == null)
        {
            return BadRequest();
        }

        return Ok(challenge);
    }

    [HttpGet("respond-challenge")]
    public async Task<ActionResult<Challenge>> RespondChallenge(string discordId, string challengeId, Hand hand)
    {
        var challenge = await _coinCommandsService.RespondChallenge(discordId, challengeId, hand);
        if (challenge == null)
        {
            return BadRequest();
        }

        return Ok(challenge);
    }
        
    
    [HttpGet("cancel-challenge")]
    public async Task<ActionResult<bool>> CancelChallenge(string challengeId) =>
        await _coinCommandsService.CancelChallenge(challengeId);
    
    [HttpGet("roulette-number")]

    public async Task<ActionResult<RouletteResponse?>> RouletteNumber(string discordId, long numberBet, long bet) =>
        await _coinCommandsService.RouletteNumber(discordId, numberBet, bet);
    
    [HttpGet("roulette-twelve")]

    public async Task<ActionResult<RouletteResponse?>> RouletteTwelve(string discordId, long twelveBet, long bet) =>
        await _coinCommandsService.RouletteTwelve(discordId, twelveBet, bet);
    
    [HttpGet("roulette-colour")]

    public async Task<ActionResult<RouletteResponse?>> RouletteColour(string discordId, bool isColourRedBet, long bet) =>
        await _coinCommandsService.RouletteColour(discordId, isColourRedBet, bet);
}