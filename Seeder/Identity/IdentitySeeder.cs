using AspCoreApi.Models;
using Microsoft.AspNetCore.Identity;

namespace AspCoreApi.Seeder.Identity;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
    
        var roles = new[]
    {
        new IdentityRole("Admin"),
        new IdentityRole("User"),
        new IdentityRole("Manager")
    };


        foreach (var role in roles)
        {
            var roleExist = await roleManager.RoleExistsAsync(role.Name);
            if (!roleExist)
            {
                await roleManager.CreateAsync(role);
            }
        }
    }

    public static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        // Seed admin user
        var adminUser = new ApplicationUser
        {
            UserName = "admin@example.com",
            Email = "admin@example.com",
            FullName = "Admin",
        };

        string adminPassword = "Admin@123";

        var user = await userManager.FindByEmailAsync(adminUser.Email);
        if (user == null)
        {
            var createUser = await userManager.CreateAsync(adminUser, adminPassword);
            if (createUser.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}