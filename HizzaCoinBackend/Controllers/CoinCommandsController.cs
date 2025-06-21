using Microsoft.AspNetCore.Mvc;

namespace HizzaCoinBackend.Controllers;

[ApiController]
[Route("api/coincommands")]
public class CoinCommandsController
{
    [HttpGet("coinclaim")]
    public async Task<ActionResult<int>> CoinClaim(string id)
    {
        return 5;
    }
    
    [HttpGet("coinbalance")]
    public async Task<ActionResult<int>> CoinBalance(string id)
    {
        return 5;
    }
    
    [HttpGet("coinleaderboard")]
    public async Task<ActionResult<int>> CoinLeaderboard(string id)
    {
        return 5;
    }
    
    [HttpGet("coingive")]
    public async Task<ActionResult<int>> CoinGive(string id)
    {
        return 5;
    }
    
    [HttpGet("initiate-challenge")]
    public async Task<ActionResult<int>> InitiateChallenge(string id)
    {
        return 5;
    }
    
    [HttpGet("respond-challenge")]
    public async Task<ActionResult<int>> RespondChallenge(string id)
    {
        return 5;
    }
}