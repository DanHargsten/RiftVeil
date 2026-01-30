using Microsoft.AspNetCore.Mvc;

namespace RiftVeil.Api.Controllers;

/// <summary>
/// Minimal endpoint for uptime and environment checks.
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController(IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok", env = env.EnvironmentName });
    }
}
