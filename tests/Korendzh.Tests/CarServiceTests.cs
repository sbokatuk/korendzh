using Korendzh.Infrastructure.Services;
using Korendzh.Tests.Helpers;
using Xunit;

namespace Korendzh.Tests;

public class CarServiceTests
{
    [Fact]
    public async Task GetOrCreate_creates_new_car_when_none_match()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new CarService(db);
        var actorId = Guid.NewGuid();

        var car = await sut.GetOrCreateAsync("Renault Master", "AB-1234", actorId);

        Assert.Equal("Renault Master", car.Name);
        Assert.Equal("AB-1234", car.LicensePlate);
        Assert.Equal(actorId, car.CreatedById);
        Assert.True(car.IsActive);
        Assert.Single(db.Cars);
    }

    [Fact]
    public async Task GetOrCreate_returns_existing_when_name_and_plate_match()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new CarService(db);
        var actorId = Guid.NewGuid();

        var first = await sut.GetOrCreateAsync("Volvo FH", "1234XY", actorId);
        var second = await sut.GetOrCreateAsync("Volvo FH", "1234XY", Guid.NewGuid());

        Assert.Equal(first.Id, second.Id);
        Assert.Single(db.Cars);
    }

    [Fact]
    public async Task Search_filters_by_name_substring_and_active_flag()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new CarService(db);
        var actor = Guid.NewGuid();

        await sut.GetOrCreateAsync("Mercedes Sprinter", "1111AB", actor);
        await sut.GetOrCreateAsync("Renault Master", "2222CD", actor);
        var inactive = await sut.GetOrCreateAsync("MAN TGE", "3333EF", actor);
        inactive.IsActive = false;
        await db.SaveChangesAsync();

        var results = await sut.SearchAsync("ren");

        Assert.Single(results);
        Assert.Equal("Renault Master", results[0].Name);
    }
}
