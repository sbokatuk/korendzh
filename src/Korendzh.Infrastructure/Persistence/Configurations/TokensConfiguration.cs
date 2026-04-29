using Korendzh.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Korendzh.Infrastructure.Persistence.Configurations;

public class InvitationTokenConfiguration : IEntityTypeConfiguration<InvitationToken>
{
    public void Configure(EntityTypeBuilder<InvitationToken> b)
    {
        b.ToTable("InvitationTokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.ExpiresAt);
    }
}

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> b)
    {
        b.ToTable("PasswordResetTokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.ExpiresAt);
    }
}

public class PushDeviceConfiguration : IEntityTypeConfiguration<PushDevice>
{
    public void Configure(EntityTypeBuilder<PushDevice> b)
    {
        b.ToTable("PushDevices");
        b.HasKey(x => x.Id);
        b.Property(x => x.PushToken).IsRequired().HasMaxLength(512);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.PushToken);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> b)
    {
        b.ToTable("AuditLog");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityType).IsRequired().HasMaxLength(64);
        b.Property(x => x.EntityId).IsRequired().HasMaxLength(64);
        b.HasIndex(x => new { x.EntityType, x.EntityId, x.At });
        b.HasIndex(x => x.At);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<NotificationLogEntry>
{
    public void Configure(EntityTypeBuilder<NotificationLogEntry> b)
    {
        b.ToTable("Notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.TemplateTag).IsRequired().HasMaxLength(64);
        b.Property(x => x.EventKey).IsRequired().HasMaxLength(128);
        b.Property(x => x.PayloadJson).IsRequired();
        b.HasIndex(x => x.EventKey).IsUnique();
        b.HasIndex(x => new { x.Status, x.NextAttemptAt });
        b.HasIndex(x => x.UserId);
    }
}
