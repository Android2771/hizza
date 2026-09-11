using HizzaCoinBackend.Models;
using HizzaCoinBackend.Models.DTOs;
using HizzaCoinBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace HizzaCoinBackend.Controllers;

[ApiController]
[Route("api/statistics")]
public class StatisticsController : ControllerBase
{
    private readonly StatisticsService _statisticsService;

    public StatisticsController(StatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet]
    public async Task<ActionResult<StatisticsResponse?>> GetStatistics() =>
        await _statisticsService.GetStats();
}