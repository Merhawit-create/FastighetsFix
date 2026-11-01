using System.ComponentModel.DataAnnotations;
using FastighetsFix.Application.Authentication;
using FastighetsFix.Application.Authentication.Dtos;
using FastighetsFix.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace FastighetsFix.Infrastructure.Authentication;

public class LoginService : ILoginService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        request.Email = request.Email?.Trim() ?? "";

        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        if (!isValid)
        {
            return new LoginResult();
        }

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return new LoginResult();
        }

        // Vi bygger inte tvåfaktorsflödet i denna del.
        // Konton med aktiverad tvåfaktor ska inte släppas igenom.
        if (await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return new LoginResult();
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            return new LoginResult();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new LoginResult
        {
            User = new CurrentUserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                Roles = roles.ToArray()
            }
        };
    }
}