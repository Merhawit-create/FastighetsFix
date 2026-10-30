namespace FastighetsFix.Application.Authentication.Dtos;

public class RegistrationResult
{
    public bool Succeeded { get; init; }
    public Dictionary<string, string[]> Errors { get; init; } = new();
}