using HizzaCoinBackend.Models;
using HizzaCoinBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace HizzaCoinBackend.Controllers;

[ApiController]
[Route("api/challenges")]
public class ChallengesController : ControllerBase
{
    private readonly ChallengesService _challengesService;
    
    public ChallengesController(ChallengesService challengesService) =>
        _challengesService = challengesService;

    [HttpGet]
    public async Task<List<Challenge>> Get() =>
        await _challengesService.GetAsync();

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<Challenge>> Get(string id)
    {
        var challenge = await _challengesService.GetAsync(id);

        if (challenge is null)
        {
            return NotFound();
        }

        return challenge;
    }
    
    [HttpPost]
    public async Task<IActionResult> Post(Challenge newChallenge)
    {
        await _challengesService.CreateAsync(newChallenge);

        return CreatedAtAction(nameof(Get), new { id = newChallenge.Id }, newChallenge);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> Update(string id, Challenge updatedChallenge)
    {
        var challenge = await _challengesService.GetAsync(id);

        if (challenge is null)
        {
            return NotFound();
        }

        updatedChallenge.Id = challenge.Id;

        await _challengesService.UpdateAsync(id, updatedChallenge);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id)
    {
        var challenge = await _challengesService.GetAsync(id);

        if (challenge is null)
        {
            return NotFound();
        }

        await _challengesService.RemoveAsync(id);

        return NoContent();
    }
}