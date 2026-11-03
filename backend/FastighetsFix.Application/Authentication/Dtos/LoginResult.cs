namespace FastighetsFix.Application.Authentication.Dtos;

public class LoginResult
{
    public CurrentUserResponse? User { get; init; }
    public string SecurityStamp { get; init; } = "";

    public bool Succeeded =>
        User is not null
        && !string.IsNullOrWhiteSpace(SecurityStamp);
}