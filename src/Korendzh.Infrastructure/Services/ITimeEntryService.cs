using Korendzh.Domain;

namespace Korendzh.Infrastructure.Services;

public interface ITimeEntryService
{
    Task<TimeEntry> CreateAsync(TimeEntry entry, Guid actorId, CancellationToken ct = default);
    Task<TimeEntry?> UpdateAsync(TimeEntry entry, Guid actorId, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid id, Guid actorId, CancellationToken ct = default);
}
