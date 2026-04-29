using Korendzh.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Korendzh.Infrastructure.Persistence.Configurations;

public class CarConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> b)
    {
        b.ToTable("Cars");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.LicensePlate).HasMaxLength(20);
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.HasIndex(x => x.Name);
        b.HasIndex(x => x.LicensePlate);
        b.HasIndex(x => x.IsActive);
    }
}
