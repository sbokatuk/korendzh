using Korendzh.Domain;
using Korendzh.Domain.Cms;
using Korendzh.Domain.Plan;
using Korendzh.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<InvitationToken> InvitationTokens => Set<InvitationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<PushDevice> PushDevices => Set<PushDevice>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();
    public DbSet<NotificationLogEntry> Notifications => Set<NotificationLogEntry>();

    // CMS / публичный лендинг.
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    // Учёт планов.
    public DbSet<PlanEntry> PlanEntries => Set<PlanEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all IEntityTypeConfiguration<T> from this assembly.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Identity tables — slimmer names without "AspNet" prefix, optional cosmetic touch.
        builder.Entity<AppUser>().ToTable("Users");
        builder.Entity<AppRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("UserTokens");
    }
}
