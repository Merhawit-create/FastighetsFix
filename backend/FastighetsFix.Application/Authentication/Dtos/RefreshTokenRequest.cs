using System.ComponentModel.DataAnnotations;

namespace FastighetsFix.Application.Authentication.Dtos;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = "";
}