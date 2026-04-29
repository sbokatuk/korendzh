using Korendzh.Domain.Plan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Korendzh.Infrastructure.Persistence.Configurations;

public class PlanEntryConfiguration : IEntityTypeConfiguration<PlanEntry>
{
    public void Configure(EntityTypeBuilder<PlanEntry> b)
    {
        b.ToTable("PlanEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.PlannedHours).HasPrecision(5, 2).IsRequired();
        b.HasIndex(x => new { x.WorkerId, x.WorkDate }).IsUnique();
        b.HasIndex(x => x.WorkDate);
    }
}
