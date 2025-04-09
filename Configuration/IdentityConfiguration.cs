using AspCoreApi.Data;
using AspCoreApi.Helpers;
using AspCoreApi.Models;
using AspCoreApi.Services;
using Microsoft.AspNetCore.Identity;

namespace AspCoreApi.Configuration;

public static class IdentityConfiguration
{
    public static IServiceCollection AddApplicationIdentity(this IServiceCollection services, IConfiguration configuration)
    {

        services
            .AddIdentity<ApplicationUser, IdentityRole>(ConfigureIdentityOptions)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI();


        // Add services for auditing identity operations
        services.AddScoped<IAuditableIdentityContext, AuditableIdentityContext>();
        // Register your custom email sender specifically for ApplicationUser
     //   services.AddSingleton<Microsoft.AspNetCore.Identity.IEmailSender<ApplicationUser>, DummyEmailSender<ApplicationUser>>();
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddScoped<IEmailServis, EmailKirim>();
        return services;
    }

    private static void ConfigureIdentityOptions(IdentityOptions options)
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 1;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
    }
}