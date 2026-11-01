using System.ComponentModel.DataAnnotations;
using FastighetsFix.Application.Authentication;
using FastighetsFix.Application.Authentication.Dtos;
using FastighetsFix.Infrastructure.Identity;
using FastighetsFix.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace FastighetsFix.Infrastructure.Authentication;

public class RegistrationService : IRegistrationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public RegistrationService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext db)
    { 
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    public async Task<RegistrationResult> RegisterAsync(RegisterRequest request)
    {
        // Rensa namn och e-post, men ändra aldrig lösenordet.
        request.FirstName = request.FirstName?.Trim() ?? "";
        request.LastName = request.LastName?.Trim() ?? "";
        request.Email = request.Email?.Trim() ?? "";

        // Kör DTO:ns valideringsregler.
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        if (!isValid)
        {
            var errors = validationResults
                .SelectMany(result => result.MemberNames.DefaultIfEmpty("Registration"),
                           (result, member) => new { Field = member, Message = result.ErrorMessage ?? "Ogiltigt värde." })
                .GroupBy(x => x.Field)
                .ToDictionary(group => group.Key,
                               group => group.Select(x => x.Message).ToArray());
            return new RegistrationResult
            {
                Succeeded = false,
                Errors = errors
            };
        }

        if (!await _roleManager.RoleExistsAsync(AppRoles.Tenant))
        {
            throw new InvalidOperationException(
                "Rollen Tenant saknas. Skapa systemets roller först.");
        }
        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email
        };
        // Användaren och rollen ska sparas tillsammans.
        await using var transaction = await _db.Database.BeginTransactionAsync();
        
        var createResult = await _userManager.CreateAsync(user, request.Password);
        
        if (!createResult.Succeeded)
        {
            return FromIdentityErrors(createResult);
        }
        var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.Tenant);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Kunde inte tilldela rollen Tenant.");
        }
        await transaction.CommitAsync();
        return new RegistrationResult
        {
            Succeeded = true
        };
    }

    private static RegistrationResult FromIdentityErrors(IdentityResult result)
    {
        return new RegistrationResult
        {
            Succeeded = false,
            Errors = result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key,
                             group => group.Select(
                             error => error.Description).ToArray())
        };
    }
}