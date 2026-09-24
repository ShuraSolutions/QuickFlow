using Microsoft.AspNetCore.Mvc;
using QuickFlow.Api.Contracts;
using QuickFlow.Api.Services;

namespace QuickFlow.Api.Controllers;

/// <summary>Task management: CRUD, complete, archive/restore, search, filters and sorting.</summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly TaskService _tasks;

    public TasksController(TaskService tasks) => _tasks = tasks;

    /// <summary>Lists tasks. Archived tasks are excluded unless <c>archived</c> says otherwise.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<List<TaskResponse>> List([FromQuery] TaskQuery query, CancellationToken ct) => _tasks.ListAsync(query, ct);

    /// <summary>Gets one task (archived tasks included).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<TaskResponse> Get(int id, CancellationToken ct) => _tasks.GetAsync(id, ct);

    /// <summary>Creates a task.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var task = await _tasks.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    /// <summary>Updates a task's title, description, status, priority and due date.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<TaskResponse> Update(int id, UpdateTaskRequest request, CancellationToken ct) => _tasks.UpdateAsync(id, request, ct);

    /// <summary>Marks a task as completed (status Done).</summary>
    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<TaskResponse> Complete(int id, CancellationToken ct) => _tasks.CompleteAsync(id, ct);

    /// <summary>Archives a task (hidden from the default list).</summary>
    [HttpPost("{id:int}/archive")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<TaskResponse> Archive(int id, CancellationToken ct) => _tasks.ArchiveAsync(id, ct);

    /// <summary>Restores an archived task.</summary>
    [HttpPost("{id:int}/restore")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<TaskResponse> Restore(int id, CancellationToken ct) => _tasks.RestoreAsync(id, ct);

    /// <summary>Permanently deletes a task.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _tasks.DeleteAsync(id, ct);
        return NoContent();
    }
}
