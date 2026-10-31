using FastighetsFix.Api.Authorization;

namespace FastighetsFix.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization(
                AuthorizationConfiguration.AdminOnly);

        group.MapGet("/access-check", () =>
            Results.Ok(new
            {
                message = "Du har administratörsbehörighet."
            }));

        return endpoints;
    }
}