using Microsoft.AspNetCore.Mvc;
using QuickFlow.Api.Data;

namespace QuickFlow.Api.Controllers;

/// <summary>Liveness and database connectivity check.</summary>
[ApiController]
[Route("health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly QuickFlowDbContext _db;

    public HealthController(QuickFlowDbContext db) => _db = db;

    /// <summary>Returns 200 when the API is up and the database is reachable, 503 otherwise.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken ct)
    {
        var connected = await _db.Database.CanConnectAsync(ct);
        var body = new HealthResponse(connected ? "Healthy" : "Unhealthy", connected ? "Connected" : "Unavailable", DateTime.UtcNow);
        return connected ? Ok(body) : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}

/// <summary>Health status payload.</summary>
public record HealthResponse(string Status, string Database, DateTime ServerTimeUtc);
