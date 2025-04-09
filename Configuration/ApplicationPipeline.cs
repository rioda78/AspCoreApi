using AspCoreApi.Data;
using AspCoreApi.Middleware;
using AspCoreApi.Models;
using AspCoreApi.Seeder.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AspCoreApi.Configuration;

public static class ApplicationPipeline
{
    public static void ConfigureApplicationPipeline(this WebApplication app, IWebHostEnvironment environment)
    {
        // Configure environment-specific settings
        if (environment.IsDevelopment())
        {
            Log.Information("Configuring for Development environment");
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Boilerplate V1");
                c.RoutePrefix = string.Empty; // Set Swagger UI at the root URL
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            });

            // Enable detailed exception pages in development
            app.UseDeveloperExceptionPage();
        }
        else
        {
            Log.Information("Configuring for Production environment");
            // Add production exception handling
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        // Add global exception handler middleware
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        // Add request logging middleware
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());

                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
                }
            };
        });

        // Add response compression
        app.UseResponseCompression();

        // Apply middleware in the correct order
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseCors("CorsPolicy");
        app.UseAuthentication();
        app.UseAuthorization();

        // Health checks endpoint
        app.MapHealthChecks("/health").AllowAnonymous();

        // Map endpoints
        app.MapGroup("/api/v1")
            .MapIdentityApi<ApplicationUser>()
            .RequireAuthorization();

        app.MapControllers();
    }

    public static async Task SeedDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            Log.Information("Starting database seeding");

            // Migrate the database if needed
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            Log.Information("Applying database migrations");
            await dbContext.Database.MigrateAsync();

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            Log.Information("Seeding roles");
            await IdentitySeeder.SeedRolesAsync(roleManager);

            Log.Information("Seeding admin user");
            await IdentitySeeder.SeedAdminUserAsync(userManager);

            Log.Information("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while seeding the database");
        }
    }
}