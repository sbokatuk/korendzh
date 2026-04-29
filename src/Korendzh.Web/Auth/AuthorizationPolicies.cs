using Korendzh.Domain;
using Microsoft.AspNetCore.Authorization;

namespace Korendzh.Web.Auth;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string ManagerOrAdmin = "ManagerOrAdmin";
    public const string AnyAuthenticated = "AnyAuthenticated";

    /// <summary>
    /// Совместима с сигнатурой <c>Action&lt;AuthorizationOptions&gt;</c> — передаётся в <c>AddAuthorization(...)</c> как method group.
    /// </summary>
    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, p => p.RequireRole(Roles.Admin));
        options.AddPolicy(ManagerOrAdmin, p => p.RequireRole(Roles.Admin, Roles.Manager));
        options.AddPolicy(AnyAuthenticated, p => p.RequireAuthenticatedUser());
    }
}
