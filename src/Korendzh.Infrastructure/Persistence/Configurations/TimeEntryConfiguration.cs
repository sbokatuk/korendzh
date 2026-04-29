using Korendzh.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Korendzh.Infrastructure.Persistence.Configurations;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> b)
    {
        b.ToTable("TimeEntries");
        b.HasKey(x => x.Id);

        b.Property(x => x.WorkDate).IsRequired();
        b.Property(x => x.Hours).HasPrecision(5, 2).IsRequired();
        b.Property(x => x.TaskName).IsRequired().HasMaxLength(200);
        b.Property(x => x.LicensePlate).HasMaxLength(20);
        b.Property(x => x.Description).HasMaxLength(500);

        b.Property(x => x.RowVersion).IsRowVersion();

        // Soft delete: глобальный фильтр исключает удалённые записи из обычных запросов.
        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasIndex(x => new { x.WorkerId, x.WorkDate });
        b.HasIndex(x => x.WorkDate);
        b.HasIndex(x => x.CarId);
        b.HasIndex(x => x.IsDeleted);
    }
}
