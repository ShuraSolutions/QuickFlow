using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickFlow.Domain.Plans;

namespace QuickFlow.Api.Data.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> b)
    {
        b.ToTable("Plans");
        b.HasKey(p => p.Id);
        b.Property(p => p.Title).IsRequired().HasMaxLength(Plan.TitleMaxLength);
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(p => p.PriorityOrder);
        b.HasIndex(p => p.StartDateTime);
        b.HasMany(p => p.Items).WithOne().HasForeignKey(i => i.PlanId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        b.Navigation(p => p.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class PlanItemConfiguration : IEntityTypeConfiguration<PlanItem>
{
    public void Configure(EntityTypeBuilder<PlanItem> b)
    {
        b.ToTable("PlanItems");
        b.HasKey(i => i.Id);
        b.Property(i => i.SourceType).HasConversion<string>().HasMaxLength(30);
        // Polymorphic reference (SourceType, SourceId): no FK; used for BR-14 clean-up lookups.
        b.HasIndex(i => new { i.SourceType, i.SourceId });
        b.HasIndex(i => new { i.PlanId, i.SourceType, i.SourceId }).IsUnique();
    }
}
