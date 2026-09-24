using System.Text.RegularExpressions;
using QuickFlow.Domain.Common;

namespace QuickFlow.Domain.Settings;

public enum DefaultView { Dashboard, Tasks, Habits, LearningResources, TodoPlans, Settings }

public enum ThemePreference { Light, Dark, System }

/// <summary>The single user's profile and application preferences (one row, Id = 1).</summary>
public class UserSettings
{
    public const int SingletonId = 1;
    public const int DisplayNameMaxLength = 100;
    public const int EmailMaxLength = 200;
    public const int MaxNotificationLeadMinutes = 1440;
    public const string DefaultDisplayName = "QuickFlow User";

    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public int Id { get; private set; } = SingletonId;
    public string DisplayName { get; private set; } = DefaultDisplayName;
    public string? Email { get; private set; }
    public bool PlanStartNotificationsEnabled { get; private set; } = true;
    /// <summary>Minutes before a plan's start at which the client should notify (0 = at start).</summary>
    public int NotificationLeadMinutes { get; private set; }
    public DefaultView DefaultView { get; private set; } = DefaultView.Dashboard;
    public ThemePreference Theme { get; private set; } = ThemePreference.System;
    public DateTime UpdatedAt { get; private set; }

    private UserSettings() { }

    public static UserSettings CreateDefault(DateTime utcNow) => new() { UpdatedAt = utcNow };

    /// <summary>Applies the given values; null means "keep the current value". An empty email clears it.</summary>
    public void Update(string? displayName, string? email, bool? planStartNotificationsEnabled,
        int? notificationLeadMinutes, DefaultView? defaultView, ThemePreference? theme, DateTime utcNow)
    {
        var v = new Validator();
        if (displayName is not null) v.Required(nameof(DisplayName), displayName, DisplayNameMaxLength);
        var normalizedEmail = email is null ? Email : Validator.Normalize(email);
        if (email is not null && normalizedEmail is not null)
        {
            v.Optional(nameof(Email), normalizedEmail, EmailMaxLength);
            v.When(!EmailPattern.IsMatch(normalizedEmail), nameof(Email), "Email must be a valid email address.");
        }
        v.When(notificationLeadMinutes is < 0 or > MaxNotificationLeadMinutes, nameof(NotificationLeadMinutes),
            $"NotificationLeadMinutes must be between 0 and {MaxNotificationLeadMinutes}.");
        if (defaultView is { } dv) v.DefinedEnum(nameof(DefaultView), dv);
        if (theme is { } t) v.DefinedEnum(nameof(Theme), t);
        v.ThrowIfInvalid();

        if (displayName is not null) DisplayName = displayName.Trim();
        Email = normalizedEmail;
        if (planStartNotificationsEnabled is { } enabled) PlanStartNotificationsEnabled = enabled;
        if (notificationLeadMinutes is { } lead) NotificationLeadMinutes = lead;
        if (defaultView is { } view) DefaultView = view;
        if (theme is { } th) Theme = th;
        UpdatedAt = utcNow;
    }
}
