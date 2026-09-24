using Microsoft.AspNetCore.Mvc;
using QuickFlow.Api.Contracts;
using QuickFlow.Api.Services;

namespace QuickFlow.Api.Controllers;

/// <summary>Recurring habits and their daily completions.</summary>
[ApiController]
[Route("api/habits")]
[Produces("application/json")]
public class HabitsController : ControllerBase
{
    private readonly HabitService _habits;

    public HabitsController(HabitService habits) => _habits = habits;

    /// <summary>Lists habits with their progress; filter with <c>isActive</c>.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<HabitResponse>), StatusCodes.Status200OK)]
    public Task<List<HabitResponse>> List([FromQuery] bool? isActive, CancellationToken ct) => _habits.ListAsync(isActive, ct);

    /// <summary>Gets one habit with its progress.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(HabitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<HabitResponse> Get(int id, CancellationToken ct) => _habits.GetAsync(id, ct);

    /// <summary>Creates a habit (Daily by default).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(HabitResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HabitResponse>> Create(HabitRequest request, CancellationToken ct)
    {
        var habit = await _habits.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = habit.Id }, habit);
    }

    /// <summary>Updates a habit's name, description and frequency.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(HabitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<HabitResponse> Update(int id, HabitRequest request, CancellationToken ct) => _habits.UpdateAsync(id, request, ct);

    /// <summary>Deactivates a habit (kept, but no longer tracked as active).</summary>
    [HttpPost("{id:int}/deactivate")]
    [ProducesResponseType(typeof(HabitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<HabitResponse> Deactivate(int id, CancellationToken ct) => _habits.SetActiveAsync(id, false, ct);

    /// <summary>Reactivates a habit.</summary>
    [HttpPost("{id:int}/activate")]
    [ProducesResponseType(typeof(HabitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<HabitResponse> Activate(int id, CancellationToken ct) => _habits.SetActiveAsync(id, true, ct);

    /// <summary>Permanently deletes a habit and its completions.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _habits.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Lists a habit's completions, newest first.</summary>
    [HttpGet("{id:int}/completions")]
    [ProducesResponseType(typeof(List<HabitCompletionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<List<HabitCompletionResponse>> Completions(int id, CancellationToken ct) => _habits.ListCompletionsAsync(id, ct);

    /// <summary>Marks a habit complete for a date (default today). A second completion for the same date returns 409.</summary>
    [HttpPost("{id:int}/completions")]
    [ProducesResponseType(typeof(HabitCompletionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HabitCompletionResponse>> Complete(int id, HabitCompletionRequest? request, CancellationToken ct)
    {
        var completion = await _habits.CompleteAsync(id, request?.Date, ct);
        return CreatedAtAction(nameof(Completions), new { id }, completion);
    }

    /// <summary>Removes the completion of a habit for a date (yyyy-MM-dd).</summary>
    [HttpDelete("{id:int}/completions/{date}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Uncomplete(int id, DateOnly date, CancellationToken ct)
    {
        await _habits.UncompleteAsync(id, date, ct);
        return NoContent();
    }
}
