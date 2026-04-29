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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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
    opt.Conventions.AuthorizeFolder("/");
    opt.Conventions.AllowAnonymousToFolder("/Account");
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
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var seeder = sp.GetRequiredService<DataSeeder>();
    await seeder.RunAsync();
}

app.Run();

public partial class Program { }
