using HizzaCoinBackend.Models;
using HizzaCoinBackend.Models.DTOs;
using HizzaCoinBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace HizzaCoinBackend.Controllers;

[ApiController]
[Route("api/statistics")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet]
    public async Task<ActionResult<StatisticsResponse?>> GetStatistics() =>
        await _statisticsService.GetStats();
}