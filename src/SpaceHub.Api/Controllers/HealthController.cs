using Microsoft.AspNetCore.Mvc;
using SpaceHub.Api.DTOs;

namespace SpaceHub.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponse> Get() => Ok(new HealthResponse("ok", "foundation"));
}
