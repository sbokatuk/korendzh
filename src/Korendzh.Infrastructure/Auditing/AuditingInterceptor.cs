using System.Text.Json;
using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Korendzh.Infrastructure.Auditing;

/// <summary>
/// SaveChangesInterceptor: автоматически проставляет CreatedAt/CreatedBy/UpdatedAt/UpdatedBy
/// для сущностей с этими полями, и пишет AuditLogEntry для значимых изменений (TimeEntry/User/Division/Car).
/// См. docs/data-model.md, docs/notifications.md (через NotificationDispatcher).
/// </summary>
public class AuditingInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<Type> AuditableTypes = new()
    {
        typeof(TimeEntry),
        typeof(Division),
        typeof(Car),
        typeof(AppUser),
    };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false
    };

    private readonly ICurrentUser _currentUser;

    public AuditingInterceptor(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var actorId = _currentUser.UserId;
        var nowUtc = DateTime.UtcNow;
        var auditEntries = new List<AuditLogEntry>();

        foreach (var entry in ctx.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLogEntry) continue;
            if (entry.Entity is NotificationLogEntry) continue;

            // Auto-populate audit-style fields where present.
            ApplyAuditFields(entry, actorId, nowUtc);

            if (!AuditableTypes.Contains(entry.Entity.GetType())) continue;
            if (entry.State is EntityState.Detached or EntityState.Unchanged) continue;

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Modified => DetectSoftDelete(entry) ? AuditAction.Deleted : AuditAction.Updated,
                EntityState.Deleted => AuditAction.Deleted,
                _ => AuditAction.Updated
            };

            var idVal = entry.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?
                .CurrentValue?.ToString() ?? string.Empty;

            auditEntries.Add(new AuditLogEntry
            {
                EntityType = entry.Entity.GetType().Name,
                EntityId = idVal,
                Action = action,
                ActorId = actorId,
                At = nowUtc,
                BeforeJson = action != AuditAction.Created ? Snapshot(entry, useOriginal: true) : null,
                AfterJson = action != AuditAction.Deleted ? Snapshot(entry, useOriginal: false) : null,
            });
        }

        if (auditEntries.Count > 0)
        {
            ctx.Set<AuditLogEntry>().AddRange(auditEntries);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplyAuditFields(EntityEntry entry, Guid? actorId, DateTime nowUtc)
    {
        if (entry.State == EntityState.Added)
        {
            if (entry.Metadata.FindProperty("CreatedAt") is not null)
                entry.Property("CreatedAt").CurrentValue = nowUtc;
            if (entry.Metadata.FindProperty("CreatedById") is not null && actorId.HasValue)
                entry.Property("CreatedById").CurrentValue = actorId.Value;
        }
        else if (entry.State == EntityState.Modified)
        {
            if (entry.Metadata.FindProperty("UpdatedAt") is not null)
                entry.Property("UpdatedAt").CurrentValue = nowUtc;
            if (entry.Metadata.FindProperty("UpdatedById") is not null && actorId.HasValue)
                entry.Property("UpdatedById").CurrentValue = actorId.Value;
        }
    }

    private static bool DetectSoftDelete(EntityEntry entry)
    {
        var deletedProp = entry.Metadata.FindProperty("IsDeleted");
        if (deletedProp is null) return false;

        var pe = entry.Property("IsDeleted");
        var current = pe.CurrentValue is bool cb && cb;
        var original = pe.OriginalValue is bool ob && ob;
        return current && !original;
    }

    private static string Snapshot(EntityEntry entry, bool useOriginal)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            // Don't dump heavy/version/concurrency tokens or password hashes.
            var name = prop.Metadata.Name;
            if (name is "PasswordHash" or "RowVersion" or "SecurityStamp" or "ConcurrencyStamp") continue;
            dict[name] = useOriginal ? prop.OriginalValue : prop.CurrentValue;
        }
        return JsonSerializer.Serialize(dict, JsonOpts);
    }
}
