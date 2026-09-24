using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickFlow.Domain.Habits;

namespace QuickFlow.Api.Data.Configurations;

public class HabitConfiguration : IEntityTypeConfiguration<Habit>
{
    public void Configure(EntityTypeBuilder<Habit> b)
    {
        b.ToTable("Habits");
        b.HasKey(h => h.Id);
        b.Property(h => h.Name).IsRequired().HasMaxLength(Habit.NameMaxLength);
        b.Property(h => h.Description).HasMaxLength(Habit.DescriptionMaxLength);
        b.Property(h => h.Frequency).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(h => h.IsActive);
        b.HasMany(h => h.Completions)
            .WithOne()
            .HasForeignKey(c => c.HabitId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(h => h.Completions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class HabitCompletionConfiguration : IEntityTypeConfiguration<HabitCompletion>
{
    public void Configure(EntityTypeBuilder<HabitCompletion> b)
    {
        b.ToTable("HabitCompletions");
        b.HasKey(c => c.Id);
        // BR-7 / §9: one completion per habit per date.
        b.HasIndex(c => new { c.HabitId, c.CompletionDate }).IsUnique();
    }
}
