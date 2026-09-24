using Microsoft.AspNetCore.Mvc;
using QuickFlow.Api.Contracts;
using QuickFlow.Api.Services;

namespace QuickFlow.Api.Controllers;

/// <summary>Aggregated dashboard across tasks, habits, plans and learning.</summary>
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboard;

    public DashboardController(DashboardService dashboard) => _dashboard = dashboard;

    /// <summary>
    /// Returns summary metrics (tasks, habits, plans, learning) and the lists shown on the dashboard:
    /// today's, overdue and completed-today tasks, today's habits, active/upcoming plans and learning in progress.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    public Task<DashboardResponse> Get(CancellationToken ct) => _dashboard.GetAsync(ct);
}
