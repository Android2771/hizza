using HizzaCoinBackend.Models;
using HizzaCoinBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace HizzaCoinBackend.Controllers;

[ApiController]
[Route("api/destiny")]
public class DestiniesController : ControllerBase
{
    private readonly DestiniesService _destinysService;
    
    public DestiniesController(DestiniesService destinysService) =>
        _destinysService = destinysService;

    [HttpGet]
    public async Task<List<Destiny>> Get() =>
        await _destinysService.GetAsync();

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<Destiny>> Get(string id)
    {
        var destiny = await _destinysService.GetAsync(id);

        if (destiny is null)
        {
            return NotFound();
        }

        return destiny;
    }
    
    [HttpPost]
    public async Task<IActionResult> Post(Destiny newDestiny)
    {
        await _destinysService.CreateAsync(newDestiny);

        return CreatedAtAction(nameof(Get), new { id = newDestiny.Id }, newDestiny);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> Update(string id, Destiny updatedDestiny)
    {
        var destiny = await _destinysService.GetAsync(id);

        if (destiny is null)
        {
            return NotFound();
        }

        updatedDestiny.Id = destiny.Id;

        await _destinysService.UpdateAsync(id, updatedDestiny);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id)
    {
        var destiny = await _destinysService.GetAsync(id);

        if (destiny is null)
        {
            return NotFound();
        }

        await _destinysService.RemoveAsync(id);

        return NoContent();
    }
}