using Korendzh.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Korendzh.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.Property(x => x.FullName).IsRequired().HasMaxLength(150);
        b.Property(x => x.GoogleSubject).HasMaxLength(64);
        b.Property(x => x.TimeZone).HasMaxLength(64);
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.EmailNotificationsEnabled).HasDefaultValue(true);

        b.HasIndex(x => x.GoogleSubject);
        b.HasIndex(x => x.DivisionId);
        b.HasIndex(x => x.IsActive);
    }
}
