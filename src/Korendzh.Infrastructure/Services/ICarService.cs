using Korendzh.Domain;

namespace Korendzh.Infrastructure.Services;

public interface ICarService
{
    Task<IReadOnlyList<Car>> SearchAsync(string? query, int take = 20, CancellationToken ct = default);
    Task<Car> GetOrCreateAsync(string name, string? licensePlate, Guid actorId, CancellationToken ct = default);
}
