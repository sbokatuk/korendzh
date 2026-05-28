using Korendzh.Infrastructure.Auditing;
using Korendzh.Infrastructure.Auth;
using Korendzh.Infrastructure.Cms;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Notifications;
using Korendzh.Infrastructure.Persistence;
using Korendzh.Infrastructure.Plan;
using Korendzh.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Korendzh.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует EF Core, Identity, фоновые сервисы и доменные сервисы.
    /// </summary>
    public static IServiceCollection AddKorendzhInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connStr = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured");
            options.UseSqlServer(connStr, sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
        });

        services.AddScoped<AuditingInterceptor>();

        services.AddIdentity<AppUser, AppRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;

                // По умолчанию Identity режет username, если в нём есть символы вне латиницы/цифр/-_.+@.
                // У нас username = email, и домен бокатюк.бел содержит кириллицу. Пустая строка отключает
                // проверку допустимых символов; уникальность email всё равно гарантируется отдельно.
                options.User.AllowedUserNameCharacters = string.Empty;

                // Lockout отключён полностью: продакшен — небольшая команда СТО, любые неудачные попытки
                // легко превращаются в саппорт-тикет. См. docs/roles-permissions.md, docs/system-overview.md.
                // Новые юзеры создаются с LockoutEnabled=false (поле AppUser); этих настроек хватает,
                // чтобы Identity не блокировал даже теоретически. Защитно в LoginModel/DataSeeder ещё
                // одним SaveChanges разлочиваем уже существующие записи (LockoutEnd=null, AFC=0).
                options.Lockout.AllowedForNewUsers = false;
                options.Lockout.MaxFailedAccessAttempts = int.MaxValue;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.Zero;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<EmailOptions>(configuration.GetSection("Email"));

        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IPushSender, NoopPushSender>();
        services.AddHostedService<NotificationSenderService>();

        services.AddScoped<IInviteService, InviteService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<ITimeEntryService, TimeEntryService>();
        services.AddScoped<ICarService, CarService>();
        services.AddScoped<IStatisticsService, StatisticsService>();

        // CMS — публичный лендинг.
        services.AddScoped<ICmsService, CmsService>();
        services.AddScoped<IMediaService, MediaService>();

        // План загрузки.
        services.AddScoped<IPlanService, PlanService>();

        return services;
    }
}
