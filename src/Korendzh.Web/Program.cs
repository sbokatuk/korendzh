using System.Text;
using Korendzh.Infrastructure;
using Korendzh.Infrastructure.Auditing;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Korendzh.Web.Auth;
using Korendzh.Web.Configuration;
using Korendzh.Web.Seeding;
using Korendzh.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Дополнительный файл с локальными/прод-секретами, не коммитится в репозиторий и переживает деплой
// (Plesk Git pull не трогает untracked-файлы). Создаётся вручную в \httpdocs на сервере.
// См. docs/deployment.md, раздел «Секреты на проде».
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Behind IIS / Plesk reverse proxy.
// ForwardedHeadersOptions живёт в Microsoft.AspNetCore.Builder, а enum ForwardedHeaders — в Microsoft.AspNetCore.HttpOverrides.
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
});

// Режим работы приложения (Full / TrackingOnly). См. docs/app-mode.md.
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<DivisionScope>();

// DataProtection: ключи cookie/antiforgery/Identity-токенов хранятся в файловой папке, чтобы
// переживать рестарт App Pool. Без этого после каждого recycle все логины и формы инвалидируются.
// Папка App_Data/dp-keys создаётся автоматически. AppPool identity должен иметь на неё право записи.
var dpKeysDir = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "dp-keys"));
dpKeysDir.Create();
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(dpKeysDir)
    // ApplicationName — purpose string, изолирующий зашифрованные DataProtection-блобы.
    // Изменение этого значения инвалидирует ВСЕ старые auth/antiforgery/TempData куки одним движением.
    // Используется как «выключатель»: bump suffix (v2 → v3) → форс-релогин всех существующих юзеров.
    //
    // История:
    //   v1 (2026-04-29) — "Korendzh"
    //   v2 (2026-05-28) — "Korendzh-v2-2026-05-28": одноразовый bump после периода эфемерных ключей
    //                     (см. docs/deployment.md → DataProtection). Куки до этого деплоя падали с
    //                     FormatException при декодировании; bump чисто их обрывает.
    // При следующем подобном инциденте — увеличить версию (v3-…) и записать историю здесь.
    .SetApplicationName("Korendzh-v2-2026-05-28");

// TempData через Session, а не Cookie. CookieTempDataProvider шифрует TempData через DataProtection
// и кладёт в cookie — это даёт FormatException при любом расхождении ключей (например, юзер ходил
// с кукой времён эфемерного DataProtection до 29 апреля). Session хранит TempData серверно, в куки
// уходит только короткий session-ID. Class «cookie can not be loaded» больше не возникает.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.Cookie.Name = "korendzh.session";
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opt.Cookie.SameSite = SameSiteMode.Lax;
    opt.Cookie.IsEssential = true;
    opt.IdleTimeout = TimeSpan.FromHours(2);
});

builder.Services.AddKorendzhInfrastructure(builder.Configuration);

// JWT для мобильного клиента.
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<JwtTokenIssuer>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

// Cookie-based auth (Identity already wires its own; tweak login paths).
//
// Имя куки версионируем (.v2) синхронно с SetApplicationName-bump'ом DataProtection. Старая
// "korendzh.auth" в браузере остаётся, но сервер по ней искать не будет — юзер увидит Login
// один раз и получит свежую "korendzh.auth.v2", подписанную текущим ключом. См. docs/deployment.md.
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.Cookie.Name = "korendzh.auth.v2";
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opt.LoginPath = "/Account/Login";
    opt.AccessDeniedPath = "/Account/AccessDenied";
    opt.LogoutPath = "/Account/Logout";
    opt.ExpireTimeSpan = TimeSpan.FromDays(14);
    opt.SlidingExpiration = true;
});

// Bearer (для API): проверяет JWT-токен по тому же Issuer/Audience/Key, что выдаёт JwtTokenIssuer.
var authBuilder = builder.Services.AddAuthentication();

if (!string.IsNullOrEmpty(jwt.Key))
{
    authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opt =>
    {
        opt.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        opt.SaveToken = true;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
}

// Google OAuth — middleware подключается, обработка — в Pages/Account/ExternalLogin.
var googleClientId = builder.Configuration["Google:ClientId"];
var googleClientSecret = builder.Configuration["Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(opt =>
    {
        opt.ClientId = googleClientId;
        opt.ClientSecret = googleClientSecret;
        opt.SignInScheme = IdentityConstants.ExternalScheme;
    });
}

builder.Services.AddAuthorization(AuthorizationPolicies.Configure);

// Razor Pages для UI + контроллеры для API/auto-complete.
// AddSessionStateTempDataProvider() переключает TempData с CookieTempDataProvider на серверный
// session-store. Без этого TempData["StatusMessage"] шифровался DataProtection и кидал
// FormatException у юзеров, у которых в браузере осталась кука прежнего ключа.
builder.Services.AddRazorPages(opt =>
{
    // По умолчанию всё закрыто; явно открываем публичные разделы.
    opt.Conventions.AuthorizeFolder("/");
    opt.Conventions.AllowAnonymousToFolder("/Account");
    opt.Conventions.AllowAnonymousToFolder("/Services");
    opt.Conventions.AllowAnonymousToFolder("/Reviews");
    opt.Conventions.AllowAnonymousToFolder("/Contacts");
    opt.Conventions.AllowAnonymousToFolder("/Pages"); // Pages/View.cshtml для /p/{slug}
    opt.Conventions.AllowAnonymousToPage("/Index");
    opt.Conventions.AllowAnonymousToPage("/Error");
}).AddSessionStateTempDataProvider();
builder.Services.AddControllers().AddSessionStateTempDataProvider();

builder.Services.AddScoped<DataSeeder>();

// Фоновая чистка просроченных токенов.
builder.Services.AddHostedService<TokenCleanupService>();

builder.Services.AddRequestLocalization(opt =>
{
    var ru = new System.Globalization.CultureInfo("ru-RU");
    opt.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(ru);
    opt.SupportedCultures = new[] { ru };
    opt.SupportedUICultures = new[] { ru };
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();

// Session должен подключаться после UseRouting и до UseAuthentication: TempData провайдер
// читает/пишет сессию во время обработки страницы. Cookie сессии — короткий ID, не шифрованный
// блоб, поэтому FormatException на нём невозможен.
app.UseSession();

// Режим TrackingOnly: блокируем публичный лендинг, CMS и связанные пути — возвращаем 404.
// Сидится между UseRouting и UseAuthentication, чтобы аккуратно работать с роутингом, но не
// затрагивать static files / health checks.
app.Use(async (ctx, next) =>
{
    var mode = ctx.RequestServices
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<AppOptions>>()
        .Value.Mode;

    if (mode == AppMode.TrackingOnly)
    {
        var path = ctx.Request.Path.Value ?? string.Empty;
        bool blocked =
            path.StartsWith("/services", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/reviews", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/contacts", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/p/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Admin/Cms", StringComparison.OrdinalIgnoreCase);

        if (blocked)
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// Миграции и сидинг при старте.
//
// Логика:
//   1. Если в коде есть EF-миграции (папка src/Korendzh.Infrastructure/Migrations/) — применяем их.
//   2. Если миграций ещё нет (первый деплой без локального dotnet-ef) — создаём схему через
//      EnsureCreated. Это бутстрап-режим: позволяет запустить приложение без миграций,
//      но в этом случае позже, когда добавите первую миграцию, придётся либо очистить БД,
//      либо вручную «отметить» её applied через __EFMigrationsHistory.
//
// Рекомендуемый production-флоу: одна миграция «Initial» в репозитории. Тогда EnsureCreated
// никогда не вызывается, MigrateAsync применяет всё корректно.
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    var startupLogger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    var appOpt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppOptions>>().Value;
    startupLogger.LogInformation("App mode: {Mode}", appOpt.Mode);

    // DataProtection self-check: подтверждаем, что папка ключей действительно есть, доступна на запись
    // и сколько в ней файлов. Если count=0 на свежем деплое — это норма (ключ создастся при первом
    // запросе). Если count=0 на 2-й день — значит папка теряется при деплое, нужно чинить деплой.
    try
    {
        dpKeysDir.Refresh();
        var keyCount = dpKeysDir.Exists ? dpKeysDir.GetFiles("key-*.xml").Length : 0;
        bool writable;
        try
        {
            var probe = Path.Combine(dpKeysDir.FullName, ".write-probe");
            await File.WriteAllTextAsync(probe, string.Empty);
            File.Delete(probe);
            writable = true;
        }
        catch
        {
            writable = false;
        }
        startupLogger.LogInformation(
            "DataProtection keys: dir={Dir}, exists={Exists}, keyCount={Count}, writable={Writable}, applicationName=Korendzh-v2-2026-05-28",
            dpKeysDir.FullName, dpKeysDir.Exists, keyCount, writable);
        if (!writable)
        {
            startupLogger.LogWarning(
                "DataProtection keys dir is NOT writable for AppPool identity. Keys will be ephemeral — все логины будут терять силу при рестарте пула. " +
                "Проверьте права на {Dir} (нужны Read/Write для AppPool-пользователя).",
                dpKeysDir.FullName);
        }
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(ex, "DataProtection keys self-check failed");
    }

    var hasMigrations = db.Database.GetMigrations().Any();
    if (hasMigrations)
    {
        startupLogger.LogInformation("Applying EF migrations...");
        await db.Database.MigrateAsync();
    }
    else
    {
        startupLogger.LogWarning(
            "No EF migrations found in code. Falling back to EnsureCreated() — schema will be created from current model. " +
            "Add a proper migration via 'dotnet ef migrations add Initial' before iterating on the model.");
        await db.Database.EnsureCreatedAsync();
    }

    var seeder = sp.GetRequiredService<DataSeeder>();
    await seeder.RunAsync();

    // Стартовая проверка SMTP-конфигурации.
    var emailOpt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Korendzh.Infrastructure.Notifications.EmailOptions>>().Value;
    if (string.IsNullOrWhiteSpace(emailOpt.Host) ||
        string.Equals(emailOpt.Host, "smtp.example.com", StringComparison.OrdinalIgnoreCase))
    {
        startupLogger.LogWarning(
            "SMTP не настроен (Email:Host='{Host}'). Все попытки отправки писем (инвайты, сброс паролей) будут падать. " +
            "Заполните Email:* в appsettings.Local.json или переменных окружения.",
            emailOpt.Host);
    }
    else
    {
        startupLogger.LogInformation(
            "SMTP configured: host={Host}:{Port}, useStartTls={Tls}, fromAddress={From}, hasUser={HasUser}",
            emailOpt.Host, emailOpt.Port, emailOpt.UseStartTls, emailOpt.FromAddress, !string.IsNullOrEmpty(emailOpt.User));
    }
}

app.Run();

public partial class Program { }
