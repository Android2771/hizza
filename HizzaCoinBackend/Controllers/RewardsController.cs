using HizzaCoinBackend.Models;
using HizzaCoinBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace HizzaCoinBackend.Controllers;

[ApiController]
[Route("api/rewards")]
public class RewardsController : ControllerBase
{
    private readonly RewardsService _rewardsService;
    
    public RewardsController(RewardsService rewardsService) =>
        _rewardsService = rewardsService;

    [HttpGet]
    public async Task<List<Reward>> Get() =>
        await _rewardsService.GetAsync();

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<Reward>> Get(string id)
    {
        var reward = await _rewardsService.GetAsync(id);

        if (reward is null)
        {
            return NotFound();
        }

        return reward;
    }
    
    [HttpPost]
    public async Task<IActionResult> Post(Reward newReward)
    {
        await _rewardsService.CreateAsync(newReward);

        return CreatedAtAction(nameof(Get), new { id = newReward.Id }, newReward);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> Update(string id, Reward updatedReward)
    {
        var reward = await _rewardsService.GetAsync(id);

        if (reward is null)
        {
            return NotFound();
        }

        updatedReward.Id = reward.Id;

        await _rewardsService.UpdateAsync(id, updatedReward);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id)
    {
        var reward = await _rewardsService.GetAsync(id);

        if (reward is null)
        {
            return NotFound();
        }

        await _rewardsService.RemoveAsync(id);

        return NoContent();
    }
}