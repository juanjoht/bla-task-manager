using Microsoft.AspNetCore.Mvc;

namespace TaskManager.Api.Controllers;

/// <summary>Public endpoint (no token required), e.g. for uptime checks.</summary>
[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "Healthy" });
}
