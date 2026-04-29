using Korendzh.Domain;
using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Seeding;

/// <summary>
/// Идемпотентный сидинг: роли + первый админ + дефолтные настройки сайта и страницы.
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
        await ResetAdminPasswordIfRequested();
        await SeedCmsDefaults(ct);
    }

    /// <summary>
    /// Recovery-механика: если в БД уже есть пользователь с email = Seed:AdminEmail,
    /// и поднят флаг Seed:ResetAdminPasswordOnStartup=true — переустанавливаем ему пароль
    /// из текущего Seed:AdminPassword. Используется однократно для восстановления доступа,
    /// после чего флаг нужно вернуть в false и снова рестартнуть приложение.
    /// </summary>
    private async Task ResetAdminPasswordIfRequested()
    {
        var enabled = string.Equals(_config["Seed:ResetAdminPasswordOnStartup"], "true",
            StringComparison.OrdinalIgnoreCase);
        if (!enabled) return;

        var email = _config["Seed:AdminEmail"];
        var password = _config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _log.LogWarning("Seed:ResetAdminPasswordOnStartup=true, но Seed:AdminEmail/AdminPassword не заданы — пропускаем.");
            return;
        }

        var user = await _users.FindByEmailAsync(email);
        if (user is null)
        {
            _log.LogWarning("Seed:ResetAdminPasswordOnStartup=true, но пользователь {Email} не найден — пропускаем.", email);
            return;
        }

        if (await _users.HasPasswordAsync(user))
        {
            var removed = await _users.RemovePasswordAsync(user);
            if (!removed.Succeeded)
            {
                _log.LogError("Не удалось снять старый пароль для {Email}: {Errors}", email,
                    string.Join("; ", removed.Errors.Select(e => e.Description)));
                return;
            }
        }
        var added = await _users.AddPasswordAsync(user, password);
        if (!added.Succeeded)
        {
            _log.LogError("Не удалось установить новый пароль для {Email}: {Errors}", email,
                string.Join("; ", added.Errors.Select(e => e.Description)));
            return;
        }

        // На всякий случай разблокируем lockout, если он был.
        await _users.ResetAccessFailedCountAsync(user);
        if (await _users.IsLockedOutAsync(user))
        {
            await _users.SetLockoutEndDateAsync(user, null);
        }

        _log.LogWarning("Seed:ResetAdminPasswordOnStartup=true: пароль для {Email} переустановлен. " +
                        "Снимите флаг и рестартните приложение.", email);
    }

    private async Task SeedCmsDefaults(CancellationToken ct)
    {
        // SiteSettings — singleton (Id=1).
        var anySettings = await _db.SiteSettings.AsNoTracking().AnyAsync(ct);
        if (!anySettings)
        {
            _db.SiteSettings.Add(new SiteSettings
            {
                Id = 1,
                SiteName = "Korendzh",
                HeroTitle = "СТО Korendzh — ремонт без сюрпризов",
                HeroSubtitle = "Диагностика, ремонт двигателя и подвески, замена расходников. Работаем с легковыми и коммерческими авто.",
                Phone = "+375 (29) 000-00-00",
                Email = "info@бокатюк.бел",
                Address = "г. Минск, ул. Примерная, 1",
                WorkingHours = "Пн–Пт: 9:00–19:00 · Сб: 10:00–16:00 · Вс: выходной",
            });
            _log.LogInformation("Seeded default SiteSettings");
        }

        // Дефолтные страницы — только если совсем нет страниц.
        var anyPages = await _db.Pages.AsNoTracking().AnyAsync(ct);
        if (!anyPages)
        {
            _db.Pages.Add(new Page
            {
                Slug = "about",
                Title = "О нас",
                ContentHtml = "<p>СТО Korendzh — это команда механиков с многолетним опытом. Мы делаем диагностику и ремонт честно: только то, что нужно, и с понятной сметой до начала работ.</p>",
                ShowInMenu = true,
                MenuOrder = 100,
                IsPublished = true,
            });
            _db.Pages.Add(new Page
            {
                Slug = "guarantees",
                Title = "Гарантии",
                ContentHtml = "<p>На все работы — гарантия 6 месяцев. На запчасти — гарантия производителя. Если что-то пошло не так после ремонта, возвращайтесь — разберёмся бесплатно.</p>",
                ShowInMenu = true,
                MenuOrder = 200,
                IsPublished = true,
            });
            _log.LogInformation("Seeded default pages");
        }

        // Дефолтные услуги — пара примеров, чтобы лендинг не был пустым.
        var anyServices = await _db.Services.AsNoTracking().AnyAsync(ct);
        if (!anyServices)
        {
            _db.Services.AddRange(
                new Service
                {
                    Slug = "diagnostika",
                    Title = "Компьютерная диагностика",
                    ShortDescription = "Считываем ошибки, проверяем датчики, даём рекомендации.",
                    DescriptionHtml = "<p>Полная диагностика двигателя, трансмиссии, ABS, подушек безопасности через профессиональный сканер. Объясняем понятно, что и зачем.</p>",
                    PriceLabel = "от 50 руб.",
                    DisplayOrder = 10,
                    IsPublished = true,
                },
                new Service
                {
                    Slug = "to",
                    Title = "Техническое обслуживание (ТО)",
                    ShortDescription = "Замена масла, фильтров, свечей. По регламенту производителя.",
                    DescriptionHtml = "<p>Плановое ТО для легковых и коммерческих авто. Используем оригинальные расходники или качественные аналоги — на ваш выбор.</p>",
                    PriceLabel = "от 80 руб.",
                    DisplayOrder = 20,
                    IsPublished = true,
                },
                new Service
                {
                    Slug = "podveska",
                    Title = "Ремонт подвески",
                    ShortDescription = "Стойки, рычаги, сайлентблоки, шаровые. Без скрытых работ.",
                    DescriptionHtml = "<p>Делаем подвеску так, чтобы поездка была спокойной, а тормозной путь предсказуемым. Перед ремонтом всегда показываем износ.</p>",
                    PriceLabel = "по запросу",
                    DisplayOrder = 30,
                    IsPublished = true,
                });
            _log.LogInformation("Seeded sample services");
        }

        await _db.SaveChangesAsync(ct);
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
