using FastighetsFix.Application.Authentication.Dtos;

namespace FastighetsFix.Application.Authentication;

public interface IRegistrationService
{
    Task<RegistrationResult> RegisterAsync(RegisterRequest request);
}