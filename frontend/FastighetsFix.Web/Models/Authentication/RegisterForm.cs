using System.ComponentModel.DataAnnotations;

namespace FastighetsFix.Web.Models.Authentication;

public class RegisterForm
{
    [Required(ErrorMessage = "Ange förnamn.")]
    [StringLength(100)]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Ange efternamn.")]
    [StringLength(100)]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Ange e-postadress.")]
    [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
    [StringLength(256)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ange lösenord.")]
    [MinLength(10, ErrorMessage = "Lösenordet måste ha minst 10 tecken.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Bekräfta lösenordet.")]
    [Compare(
        nameof(Password),
        ErrorMessage = "Lösenorden matchar inte.")]
    public string ConfirmPassword { get; set; } = "";
}