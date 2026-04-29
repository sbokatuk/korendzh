using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Seeding;

/// <summary>
/// Идемпотентный сидинг: роли + первый админ.
/// Параметры берутся из переменных окружения (Plesk Application Settings).
/// См. docs/data-model.md (раздел «Сидинг при деплое»).
/// </summary>
public class DataSeeder
{
    private readonly AppDbContext _db;
    private readonly RoleManager<AppRole> _roles;
    private readonly UserManager<AppUser> _users;
    private readonly IConfiguration _config;
    private readonly ILogger<DataSeeder> _log;

    public DataSeeder(
        AppDbContext db,
        RoleManager<AppRole> roles,
        UserManager<AppUser> users,
        IConfiguration config,
        ILogger<DataSeeder> log)
    {
        _db = db;
        _roles = roles;
        _users = users;
        _config = config;
        _log = log;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        await SeedRoles();
        await SeedAdmin(ct);
    }

    private async Task SeedRoles()
    {
        foreach (var role in Roles.All)
        {
            if (!await _roles.RoleExistsAsync(role))
            {
                var result = await _roles.CreateAsync(new AppRole(role));
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Не удалось создать роль {role}: " +
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                _log.LogInformation("Seeded role {Role}", role);
            }
        }
    }

    private async Task SeedAdmin(CancellationToken ct)
    {
        var anyAdmin = await _db.UserRoles
            .AsNoTracking()
            .Join(_db.Roles.AsNoTracking().Where(r => r.Name == Roles.Admin),
                  ur => ur.RoleId, r => r.Id, (ur, r) => ur.UserId)
            .AnyAsync(ct);

        if (anyAdmin) return;

        var email = _config["Seed:AdminEmail"];
        var password = _config["Seed:AdminPassword"];
        var fullName = _config["Seed:AdminFullName"] ?? "System Administrator";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _log.LogWarning("Seed:AdminEmail / Seed:AdminPassword не заданы — пропускаем сидинг админа. " +
                            "Задайте переменные окружения и перезапустите приложение.");
            return;
        }

        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            IsActive = true,
        };

        var createResult = await _users.CreateAsync(admin, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException("Не удалось создать админа: " +
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        var roleResult = await _users.AddToRoleAsync(admin, Roles.Admin);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException("Не удалось назначить роль Admin: " +
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        _log.LogInformation("Seeded admin user {Email}", email);
    }
}
