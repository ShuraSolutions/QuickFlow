using QuickFlow.Domain.Common;
using QuickFlow.Domain.Settings;

namespace QuickFlow.Tests.Settings;

public class UserSettingsTests
{
    private static readonly DateTime Now = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Defaults()
    {
        var s = UserSettings.CreateDefault(Now);
        Assert.Equal(DefaultView.Dashboard, s.DefaultView);
        Assert.Equal(ThemePreference.System, s.Theme);
        Assert.True(s.PlanStartNotificationsEnabled);
        Assert.Equal(0, s.NotificationLeadMinutes);
    }

    [Fact]
    public void Invalid_email_lead_minutes_and_enums_are_rejected()
    {
        var s = UserSettings.CreateDefault(Now);
        var ex = Assert.Throws<DomainValidationException>(() =>
            s.Update(null, "not-an-email", null, -1, (DefaultView)99, (ThemePreference)9, Now));
        Assert.Contains("Email", ex.Errors.Keys);
        Assert.Contains("NotificationLeadMinutes", ex.Errors.Keys);
        Assert.Contains("DefaultView", ex.Errors.Keys);
        Assert.Contains("Theme", ex.Errors.Keys);
    }

    [Fact]
    public void Null_fields_are_unchanged_and_empty_email_clears()
    {
        var s = UserSettings.CreateDefault(Now);
        s.Update("Sam", "sam@example.com", false, 15, DefaultView.TodoPlans, ThemePreference.Dark, Now);
        s.Update(null, null, null, null, null, null, Now);
        Assert.Equal("Sam", s.DisplayName);
        Assert.Equal("sam@example.com", s.Email);
        Assert.False(s.PlanStartNotificationsEnabled);
        Assert.Equal(15, s.NotificationLeadMinutes);
        s.Update(null, "", null, null, null, null, Now);
        Assert.Null(s.Email);
    }

    [Fact]
    public void Blank_display_name_is_rejected()
    {
        Assert.Throws<DomainValidationException>(() =>
            UserSettings.CreateDefault(Now).Update(" ", null, null, null, null, null, Now));
    }
}
