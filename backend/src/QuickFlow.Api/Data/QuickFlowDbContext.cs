using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace QuickFlow.Api.Data;

public class QuickFlowDbContext : DbContext
{
    public QuickFlowDbContext(DbContextOptions<QuickFlowDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuickFlowDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Every DateTime is stored and returned as UTC, so JSON output carries a 'Z' suffix.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)) { }
    }

    private sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableUtcDateTimeConverter() : base(
            v => v == null ? v : (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()),
            v => v == null ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)) { }
    }
}
