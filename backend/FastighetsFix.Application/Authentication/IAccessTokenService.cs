namespace FastighetsFix.Application.Authentication;

public interface IAccessTokenService
{
    Task<bool> IsValidAsync(string userId, string securityStamp);
    Task<bool> RevokeAllAsync(string userId);
}