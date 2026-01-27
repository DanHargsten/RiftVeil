using Microsoft.AspNetCore.Mvc;

namespace RiftVeil.Api.Controllers;

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
