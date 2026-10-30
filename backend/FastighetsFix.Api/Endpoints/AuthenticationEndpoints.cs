using FastighetsFix.Application.Authentication;
using FastighetsFix.Application.Authentication.Dtos;

namespace FastighetsFix.Api.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/register", RegisterAsync)
            .AllowAnonymous()
            .WithName("Register")
            .WithTags("Authentication");

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request,
        IRegistrationService registrationService)
    {
        var result = await registrationService.RegisterAsync(request);

        if (!result.Succeeded)
        {
            return Results.ValidationProblem(result.Errors);
        }

        return Results.StatusCode(StatusCodes.Status201Created);
    }
}