using Korendzh.Domain;
using Korendzh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Infrastructure.Services;

public class CarService : ICarService
{
    private readonly AppDbContext _db;

    public CarService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Car>> SearchAsync(string? query, int take = 20, CancellationToken ct = default)
    {
        var q = _db.Cars.AsNoTracking().Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var t = query.Trim();
            q = q.Where(c => EF.Functions.Like(c.Name, $"%{t}%")
                          || (c.LicensePlate != null && EF.Functions.Like(c.LicensePlate, $"%{t}%")));
        }
        return await q.OrderBy(c => c.Name).Take(take).ToListAsync(ct);
    }

    public async Task<Car> GetOrCreateAsync(string name, string? licensePlate, Guid actorId, CancellationToken ct = default)
    {
        name = name.Trim();
        var trimmedPlate = string.IsNullOrWhiteSpace(licensePlate) ? null : licensePlate.Trim();

        var existing = await _db.Cars
            .Where(c => c.IsActive && c.Name == name && c.LicensePlate == trimmedPlate)
            .FirstOrDefaultAsync(ct);

        if (existing != null) return existing;

        var car = new Car
        {
            Name = name,
            LicensePlate = trimmedPlate,
            CreatedById = actorId,
            IsActive = true,
        };
        _db.Cars.Add(car);
        await _db.SaveChangesAsync(ct);
        return car;
    }
}
