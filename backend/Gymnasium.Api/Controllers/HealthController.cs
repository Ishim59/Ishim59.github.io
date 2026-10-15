using Gymnasium.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Gymnasium.Api.Controllers;

[ApiController, Route("api/health")]
public sealed class HealthController(GymnasiumDbContext database) : ControllerBase
{
    [HttpGet]
    public IActionResult Live() => Ok(new { status = "ok" });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken) =>
        await database.Database.CanConnectAsync(cancellationToken)
            && !(await database.Database.GetPendingMigrationsAsync(cancellationToken)).Any()
            ? Ok(new { status = "ready", database = "PostgreSQL" })
            : StatusCode(503, new { status = "unavailable" });
}
