using Microsoft.AspNetCore.Identity;

namespace AspCoreApi.Seeder.Identity;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        // Seed roles
        string[] roleNames = { "Admin", "User", "Manager" };

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    public static async Task SeedAdminUserAsync(UserManager<IdentityUser> userManager)
    {
        // Seed admin user
        var adminUser = new IdentityUser
        {
            UserName = "admin@example.com",
            Email = "admin@example.com",
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