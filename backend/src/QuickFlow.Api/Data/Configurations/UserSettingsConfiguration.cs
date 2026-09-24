using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickFlow.Domain.Settings;

namespace QuickFlow.Api.Data.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> b)
    {
        b.ToTable("UserSettings");
        b.HasKey(s => s.Id);
        b.Property(s => s.Id).ValueGeneratedNever();
        b.Property(s => s.DisplayName).IsRequired().HasMaxLength(UserSettings.DisplayNameMaxLength);
        b.Property(s => s.Email).HasMaxLength(UserSettings.EmailMaxLength);
        b.Property(s => s.DefaultView).HasConversion<string>().HasMaxLength(30);
        b.Property(s => s.Theme).HasConversion<string>().HasMaxLength(20);

        // Seed the single settings row with defaults.
        b.HasData(new
        {
            Id = UserSettings.SingletonId,
            DisplayName = UserSettings.DefaultDisplayName,
            PlanStartNotificationsEnabled = true,
            NotificationLeadMinutes = 0,
            DefaultView = DefaultView.Dashboard,
            Theme = ThemePreference.System,
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}
