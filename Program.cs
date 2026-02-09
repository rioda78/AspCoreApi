using AspCoreApi.Configuration;
using Serilog;

namespace AspCoreApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog first, before any services are built
        LoggingConfiguration.ConfigureLogger();

        try
        {
            Log.Information("Starting web application");

            var builder = WebApplication.CreateBuilder(args);

            // Add Serilog to the application
            builder.Host.UseSerilog();

            // Configure all services using extension methods
            builder.Services
                .AddApplicationLogging()
                .AddApplicationDatabase(builder.Configuration, builder.Environment)
                .AddApplicationIdentity(builder.Configuration)
                .AddApplicationSecurity(builder.Configuration)
                .AddApplicationSwagger()
                .AddResponseKompres()
                .AddApplicationRateLimiting()
                .AddApplicationCors(builder.Configuration)
                .AddProblemDetails()
                .AddControllers();

            // Add health checks separately since it returns IHealthChecksBuilder
            builder.Services.AddHealthChecks();

            // Add HTTP client and other services
            builder.Services.AddHttpClients();

            // Configure service validation based on environment
            builder.ConfigureServiceValidation();

            var app = builder.Build();

            // Configure middleware pipeline
            app.ConfigureApplicationPipeline(builder.Environment);

            // Pastikan seeding tidak menghentikan Build Migrasi
    
          //  await app.SeedDatabase();


            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}