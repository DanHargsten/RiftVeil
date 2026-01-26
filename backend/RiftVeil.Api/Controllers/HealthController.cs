using Microsoft.AspNetCore.Mvc;

namespace RiftVeil.Api.Controllers;

[ApiController]
[Route("api/controller")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "Success!" });
    }
}
