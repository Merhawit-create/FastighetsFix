using FastighetsFix.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace FastighetsFix.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public int? ApartmentId { get; set; }

    public Apartment? Apartment { get; set; }
}