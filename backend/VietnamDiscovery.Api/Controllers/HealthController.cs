using Microsoft.AspNetCore.Mvc;
using VietnamDiscovery.Api.Services.Interfaces;

namespace VietnamDiscovery.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_healthService.GetStatus());
    }
}