using Microsoft.AspNetCore.Mvc;
using QuickFlow.Api.Contracts;
using QuickFlow.Api.Services;
using QuickFlow.Domain.Plans;

namespace QuickFlow.Api.Controllers;

/// <summary>Todo plans: time-boxed plans built from existing tasks, habits and learning resources.</summary>
[ApiController]
[Route("api/plans")]
[Produces("application/json")]
public class PlansController : ControllerBase
{
    private readonly PlanService _plans;

    public PlansController(PlanService plans) => _plans = plans;

    /// <summary>Lists plans ordered by priority order (then start time), optionally filtered by computed status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<List<PlanResponse>> List([FromQuery] PlanStatus? status, CancellationToken ct) => _plans.ListAsync(status, ct);

    /// <summary>History of completed plans (ended or all items done) with how much of each was completed.</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<PlanResponse>), StatusCodes.Status200OK)]
    public Task<List<PlanResponse>> History(CancellationToken ct) => _plans.HistoryAsync(ct);

    /// <summary>Plans whose start time has been reached (and not yet ended) and whose start notification was not acknowledged.</summary>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(List<PlanResponse>), StatusCodes.Status200OK)]
    public Task<List<PlanResponse>> Notifications(CancellationToken ct) => _plans.PendingNotificationsAsync(ct);

    /// <summary>Acknowledges a plan's start notification so it is not reported again.</summary>
    [HttpPost("{id:int}/notifications/ack")]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<PlanResponse> AcknowledgeStart(int id, CancellationToken ct) => _plans.AcknowledgeStartAsync(id, ct);

    /// <summary>Gets one plan with its items and computed progress.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<PlanResponse> Get(int id, CancellationToken ct) => _plans.GetAsync(id, ct);

    /// <summary>Creates a plan from existing tasks, habits and/or learning resources.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanResponse>> Create(PlanRequest request, CancellationToken ct)
    {
        var plan = await _plans.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = plan.Id }, plan);
    }

    /// <summary>Updates a plan's fields and items (retained items keep their done flag).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<PlanResponse> Update(int id, PlanRequest request, CancellationToken ct) => _plans.UpdateAsync(id, request, ct);

    /// <summary>Removes a plan (its items are removed; the referenced tasks/habits/cards are kept).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _plans.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Marks a plan item done or not done. Returns the updated plan (progress recomputed).
    /// The source entity's own completion follows (BR-13): task Done/Todo, habit completion for today, card Completed/InProgress.
    /// </summary>
    [HttpPatch("{id:int}/items/{itemId:int}")]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<PlanResponse> SetItemDone(int id, int itemId, UpdatePlanItemRequest request, CancellationToken ct) =>
        _plans.SetItemDoneAsync(id, itemId, request.IsDone, ct);
}
