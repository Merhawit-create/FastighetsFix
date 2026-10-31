using FastighetsFix.Infrastructure.Identity;

namespace FastighetsFix.Api.Authorization;

public static class AuthorizationConfiguration
{
    public const string AdminOnly = "AdminOnly";

    public static IServiceCollection AddAppAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminOnly, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(AppRoles.Admin);
            });
        });

        return services;
    }
}