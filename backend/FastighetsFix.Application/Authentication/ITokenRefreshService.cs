using FastighetsFix.Application.Authentication.Dtos;

namespace FastighetsFix.Application.Authentication;

public interface ITokenRefreshService
{
    Task<LoginResult> RefreshAsync(RefreshTokenRequest request);
}