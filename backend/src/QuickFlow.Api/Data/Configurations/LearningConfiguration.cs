using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickFlow.Domain.Learning;

namespace QuickFlow.Api.Data.Configurations;

public class LearningCardConfiguration : IEntityTypeConfiguration<LearningCard>
{
    public void Configure(EntityTypeBuilder<LearningCard> b)
    {
        b.ToTable("LearningCards");
        b.HasKey(c => c.Id);
        b.Property(c => c.Title).IsRequired().HasMaxLength(LearningCard.TitleMaxLength);
        b.Property(c => c.Description).HasMaxLength(LearningCard.DescriptionMaxLength);
        b.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(c => c.Status);
        b.Ignore(c => c.MilestonesDone);
        b.Ignore(c => c.MilestoneProgressPercentage);

        // BR-9: every milestone/note has exactly one (required) card; removed with the card.
        b.HasMany(c => c.Milestones).WithOne().HasForeignKey(m => m.LearningCardId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        b.HasMany(c => c.Notes).WithOne().HasForeignKey(n => n.LearningCardId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        b.Navigation(c => c.Milestones).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(c => c.Notes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class LearningMilestoneConfiguration : IEntityTypeConfiguration<LearningMilestone>
{
    public void Configure(EntityTypeBuilder<LearningMilestone> b)
    {
        b.ToTable("LearningMilestones");
        b.HasKey(m => m.Id);
        b.Property(m => m.Title).IsRequired().HasMaxLength(LearningMilestone.TitleMaxLength);
    }
}

public class LearningNoteConfiguration : IEntityTypeConfiguration<LearningNote>
{
    public void Configure(EntityTypeBuilder<LearningNote> b)
    {
        b.ToTable("LearningNotes");
        b.HasKey(n => n.Id);
        b.Property(n => n.Text).IsRequired().HasMaxLength(LearningNote.TextMaxLength);
    }
}
