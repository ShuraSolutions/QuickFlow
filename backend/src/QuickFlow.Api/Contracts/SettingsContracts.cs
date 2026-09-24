using QuickFlow.Domain.Settings;

namespace QuickFlow.Api.Contracts;

/// <summary>User profile and application preferences.</summary>
/// <param name="NotificationLeadMinutes">Minutes before a plan's start at which to notify (0 = at start).</param>
public record SettingsResponse(
    string DisplayName,
    string? Email,
    bool PlanStartNotificationsEnabled,
    int NotificationLeadMinutes,
    DefaultView DefaultView,
    ThemePreference Theme,
    DateTime UpdatedAt)
{
    public static SettingsResponse From(UserSettings s) => new(s.DisplayName, s.Email, s.PlanStartNotificationsEnabled,
        s.NotificationLeadMinutes, s.DefaultView, s.Theme, s.UpdatedAt);
}

/// <summary>Settings update. Omitted (null) fields keep their current value; an empty email clears it.</summary>
public class UpdateSettingsRequest
{
    /// <summary>At most 100 characters; cannot be blank when provided.</summary>
    public string? DisplayName { get; set; }
    /// <summary>Valid email address, at most 200 characters; "" clears it.</summary>
    public string? Email { get; set; }
    public bool? PlanStartNotificationsEnabled { get; set; }
    /// <summary>0 to 1440.</summary>
    public int? NotificationLeadMinutes { get; set; }
    public DefaultView? DefaultView { get; set; }
    public ThemePreference? Theme { get; set; }
}
