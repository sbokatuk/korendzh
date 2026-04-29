using System.Text;
using Korendzh.Infrastructure;
using Korendzh.Infrastructure.Auditing;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Korendzh.Web.Auth;
using Korendzh.Web.Seeding;
using Korendzh.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
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
    .SetApplicationName("Korendzh");

builder.Services.AddKorendzhInfrastructure(builder.Configuration);

// JWT для мобильного клиента.
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<JwtTokenIssuer>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

// Cookie-based auth (Identity already wires its own; tweak login paths).
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.Cookie.Name = "korendzh.auth";
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
});
builder.Services.AddControllers();

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
