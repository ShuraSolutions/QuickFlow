using Microsoft.AspNetCore.Mvc;
using QuickFlow.Api.Contracts;
using QuickFlow.Api.Services;
using QuickFlow.Domain.Learning;

namespace QuickFlow.Api.Controllers;

/// <summary>Learning resources (cards) with milestones and notes.</summary>
[ApiController]
[Route("api/learning-cards")]
[Produces("application/json")]
public class LearningCardsController : ControllerBase
{
    private readonly LearningService _learning;

    public LearningCardsController(LearningService learning) => _learning = learning;

    /// <summary>Lists learning cards (newest first) with milestones and notes; filter by <c>status</c>.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<LearningCardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<List<LearningCardResponse>> List([FromQuery] LearningStatus? status, CancellationToken ct) => _learning.ListAsync(status, ct);

    /// <summary>Gets one learning card with its milestones and notes.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LearningCardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<LearningCardResponse> Get(int id, CancellationToken ct) => _learning.GetAsync(id, ct);

    /// <summary>Adds a learning card.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LearningCardResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LearningCardResponse>> Create(LearningCardRequest request, CancellationToken ct)
    {
        var card = await _learning.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = card.Id }, card);
    }

    /// <summary>Updates a learning card's title, description and status.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(LearningCardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<LearningCardResponse> Update(int id, LearningCardRequest request, CancellationToken ct) => _learning.UpdateAsync(id, request, ct);

    /// <summary>Removes a learning card with its milestones and notes.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _learning.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Adds a milestone to a learning card.</summary>
    [HttpPost("{id:int}/milestones")]
    [ProducesResponseType(typeof(LearningMilestoneResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearningMilestoneResponse>> AddMilestone(int id, CreateMilestoneRequest request, CancellationToken ct)
    {
        var milestone = await _learning.AddMilestoneAsync(id, request, ct);
        return CreatedAtAction(nameof(Get), new { id }, milestone);
    }

    /// <summary>Renames, completes/reopens or re-dates a milestone of this card.</summary>
    [HttpPatch("{id:int}/milestones/{milestoneId:int}")]
    [ProducesResponseType(typeof(LearningMilestoneResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<LearningMilestoneResponse> UpdateMilestone(int id, int milestoneId, UpdateMilestoneRequest request, CancellationToken ct) =>
        _learning.UpdateMilestoneAsync(id, milestoneId, request, ct);

    /// <summary>Removes a milestone from this card.</summary>
    [HttpDelete("{id:int}/milestones/{milestoneId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMilestone(int id, int milestoneId, CancellationToken ct)
    {
        await _learning.RemoveMilestoneAsync(id, milestoneId, ct);
        return NoContent();
    }

    /// <summary>Adds a free-form note to a learning card.</summary>
    [HttpPost("{id:int}/notes")]
    [ProducesResponseType(typeof(LearningNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearningNoteResponse>> AddNote(int id, CreateNoteRequest request, CancellationToken ct)
    {
        var note = await _learning.AddNoteAsync(id, request, ct);
        return CreatedAtAction(nameof(Get), new { id }, note);
    }

    /// <summary>Removes a note from this card.</summary>
    [HttpDelete("{id:int}/notes/{noteId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveNote(int id, int noteId, CancellationToken ct)
    {
        await _learning.RemoveNoteAsync(id, noteId, ct);
        return NoContent();
    }
}
