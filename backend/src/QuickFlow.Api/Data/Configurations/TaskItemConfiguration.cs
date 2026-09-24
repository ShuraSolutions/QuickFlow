using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickFlow.Domain.Tasks;

namespace QuickFlow.Api.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> b)
    {
        b.ToTable("Tasks");
        b.HasKey(t => t.Id);
        b.Property(t => t.Title).IsRequired().HasMaxLength(TaskItem.TitleMaxLength);
        b.Property(t => t.Description).HasMaxLength(TaskItem.DescriptionMaxLength);
        b.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(t => t.Status);
        b.HasIndex(t => t.Priority);
        b.HasIndex(t => t.DueDate);
        b.HasIndex(t => t.IsArchived);
    }
}
