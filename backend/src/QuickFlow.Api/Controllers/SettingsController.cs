using Microsoft.AspNetCore.Mvc;
using QuickFlow.Api.Contracts;
using QuickFlow.Api.Data;
using QuickFlow.Domain.Common;
using QuickFlow.Domain.Settings;

namespace QuickFlow.Api.Controllers;

/// <summary>User profile and application preferences (single record).</summary>
[ApiController]
[Route("api/settings")]
[Produces("application/json")]
public class SettingsController : ControllerBase
{
    private readonly QuickFlowDbContext _db;
    private readonly IClock _clock;

    public SettingsController(QuickFlowDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <summary>Gets the current settings.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SettingsResponse), StatusCodes.Status200OK)]
    public async Task<SettingsResponse> Get(CancellationToken ct) => SettingsResponse.From(await LoadAsync(ct));

    /// <summary>Updates settings; omitted fields are unchanged.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(SettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<SettingsResponse> Update(UpdateSettingsRequest r, CancellationToken ct)
    {
        var settings = await LoadAsync(ct);
        settings.Update(r.DisplayName, r.Email, r.PlanStartNotificationsEnabled, r.NotificationLeadMinutes,
            r.DefaultView, r.Theme, _clock.UtcNow);
        await _db.SaveChangesAsync(ct);
        return SettingsResponse.From(settings);
    }

    private async Task<UserSettings> LoadAsync(CancellationToken ct)
    {
        var settings = await _db.UserSettings.FindAsync(new object[] { UserSettings.SingletonId }, ct);
        if (settings is not null) return settings;
        // The row is seeded by the migration; recreate it if it was removed manually.
        settings = UserSettings.CreateDefault(_clock.UtcNow);
        _db.UserSettings.Add(settings);
        await _db.SaveChangesAsync(ct);
        return settings;
    }
}
