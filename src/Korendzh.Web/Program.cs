using Korendzh.Infrastructure;
using Korendzh.Infrastructure.Auditing;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Korendzh.Web.Auth;
using Korendzh.Web.Seeding;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Behind IIS / Plesk reverse proxy.
builder.Services.Configure<Microsoft.AspNetCore.HttpOverrides.ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<DivisionScope>();

builder.Services.AddKorendzhInfrastructure(builder.Configuration);

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

// Google OAuth — стаб конфигурации; ключи задаются через Application Settings (Google:ClientId/ClientSecret).
var googleClientId = builder.Configuration["Google:ClientId"];
var googleClientSecret = builder.Configuration["Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(opt =>
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
