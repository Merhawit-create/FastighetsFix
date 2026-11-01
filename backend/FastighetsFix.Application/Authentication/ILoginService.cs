using FastighetsFix.Application.Authentication.Dtos;

namespace FastighetsFix.Application.Authentication;

public interface ILoginService
{
    Task<LoginResult> LoginAsync(LoginRequest request);
}