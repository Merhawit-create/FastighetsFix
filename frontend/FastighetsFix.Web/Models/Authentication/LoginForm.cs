using System.ComponentModel.DataAnnotations;

namespace FastighetsFix.Web.Models.Authentication;

public class LoginForm
{
    [Required(ErrorMessage = "Ange e-postadress.")]
    [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ange lösenord.")]
    public string Password { get; set; } = "";
}