using HizzaCoinBackend.Models;
using HizzaCoinBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace HizzaCoinBackend.Controllers;

[ApiController]
[Route("api/roulette")]
public class RouletteController : ControllerBase
{
    private readonly RouletteService _rouletteService;
    
    public RouletteController(RouletteService rouletteService) =>
        _rouletteService = rouletteService;

    [HttpGet]
    public async Task<List<Roulette>> Get() =>
        await _rouletteService.GetAsync();

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<Roulette>> Get(string id)
    {
        var reward = await _rouletteService.GetAsync(id);

        if (reward is null)
        {
            return NotFound();
        }

        return reward;
    }
    
    [HttpPost]
    public async Task<IActionResult> Post(Roulette newRoulette)
    {
        await _rouletteService.CreateAsync(newRoulette);

        return CreatedAtAction(nameof(Get), new { id = newRoulette.Id }, newRoulette);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> Update(string id, Roulette updatedRoulette)
    {
        var reward = await _rouletteService.GetAsync(id);

        if (reward is null)
        {
            return NotFound();
        }

        updatedRoulette.Id = reward.Id;

        await _rouletteService.UpdateAsync(id, updatedRoulette);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id)
    {
        var reward = await _rouletteService.GetAsync(id);

        if (reward is null)
        {
            return NotFound();
        }

        await _rouletteService.RemoveAsync(id);

        return NoContent();
    }
}