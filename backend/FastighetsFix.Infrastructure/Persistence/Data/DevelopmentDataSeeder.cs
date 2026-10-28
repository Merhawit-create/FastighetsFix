using FastighetsFix.Domain.Entities;
using FastighetsFix.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FastighetsFix.Infrastructure.Persistence.Data;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        string password)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Roller
        string[] roles = [AppRoles.Admin, AppRoles.Technician, AppRoles.Tenant];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(role));

                EnsureSuccess(result, $"Skapa rollen {role}");
            }
        }

        // 2. Fastighet
        var property = await db.Properties.SingleOrDefaultAsync(
            x => x.Name == "Solgården"
                 && x.Address == "Exempelgatan 10");

        if (property is null)
        {
            property = new Property { Name = "Solgården", Address = "Exempelgatan 10" };
            db.Properties.Add(property);
            await db.SaveChangesAsync();
        }

        // 3. Lägenheter
        var apartment1001 = await EnsureApartmentAsync(db, property.Id, "1001");

        var apartment1002 = await EnsureApartmentAsync(db, property.Id, "1002");

        // 4. Användare och deras roller
        await EnsureUserAsync(
            userManager,
            "admin@example.com",
            "Anna",
            "Admin",
            AppRoles.Admin,
            null,
            password);

        await EnsureUserAsync(
            userManager,
            "tekniker@example.com",
            "Ali",
            "Tekniker",
            AppRoles.Technician,
            null,
            password);

        var sara = await EnsureUserAsync(
            userManager,
            "sara@example.com",
            "Sara",
            "Test",
            AppRoles.Tenant,
            apartment1001.Id,
            password);

        var adam = await EnsureUserAsync(
            userManager,
            "adam@example.com",
            "Adam",
            "Test",
            AppRoles.Tenant,
            apartment1002.Id,
            password);

        // 5. Felanmälningar
        await EnsureRequestAsync(
            db,
            sara,
            apartment1001.Id,
            "Elementet blir inte varmt",
            "Elementet i vardagsrummet är kallt.");

        await EnsureRequestAsync(
            db,
            adam,
            apartment1002.Id,
            "Kranen droppar",
            "Kökskranen droppar även när den är stängd.");
    }

    private static async Task<Apartment> EnsureApartmentAsync(
        ApplicationDbContext db,
        int propertyId,
        string number)
    {
        var apartment = await db.Apartments.SingleOrDefaultAsync(
            x => x.PropertyId == propertyId
                 && x.ApartmentNumber == number);

        if (apartment is not null)
        {
            return apartment;
        }

        apartment = new Apartment
        {
            PropertyId = propertyId,
            ApartmentNumber = number
        };

        db.Apartments.Add(apartment);
        await db.SaveChangesAsync();

        return apartment;
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string firstName,
        string lastName,
        string role,
        int? apartmentId,
        string password)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                ApartmentId = apartmentId,

                // Endast för dessa lokala demokonton.
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(
                user, password);

            EnsureSuccess(createResult, $"Skapa användaren {email}");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(
                user, role);

            EnsureSuccess(roleResult, $"Tilldela rollen {role}");
        }

        return user;
    }

    private static async Task EnsureRequestAsync(
        ApplicationDbContext db,
        ApplicationUser creator,
        int apartmentId,
        string title,
        string description)
    {
        var exists = await db.MaintenanceRequests.AnyAsync(
            x => x.CreatedById == creator.Id
                 && x.ApartmentId == apartmentId
                 && x.Title == title);

        if (exists)
        {
            return;
        }

        db.MaintenanceRequests.Add(new MaintenanceRequest
        {
            Title = title,
            Description = description,
            ApartmentId = apartmentId,
            CreatedById = creator.Id
        });

        await db.SaveChangesAsync();
    }

    private static void EnsureSuccess(
        IdentityResult result,
        string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(x => x.Description));

            throw new InvalidOperationException(
                $"{operation} misslyckades: {errors}");
        }
    }
}