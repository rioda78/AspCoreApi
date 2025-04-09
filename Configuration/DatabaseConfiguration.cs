using AspCoreApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;

namespace AspCoreApi.Configuration;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddApplicationDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var databaseType = configuration.GetValue<string>("rdms")?.ToLowerInvariant() ?? "mysql";
        Log.Information("Configuring database connection for {DatabaseType}", databaseType);

        switch (databaseType)
        {
            case "mysql":
                ConfigureMySqlDatabase(services, configuration, environment);
                break;
            case "firebird":
                ConfigureFirebirdDatabase(services, configuration, environment);
                break;
            default:
                Log.Error("Unsupported database type: {DatabaseType}", databaseType);
                throw new InvalidOperationException($"Unsupported database type: {databaseType}");
        }

        return services;
    }

    private static void ConfigureMySqlDatabase(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("MysqlKoneksi");

        if (string.IsNullOrEmpty(connectionString))
        {
            Log.Error("MySQL connection string is not configured");
            throw new InvalidOperationException("MySQL connection string is not configured.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString),
                    sqlOptions => sqlOptions
                        .EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null)
                        .CommandTimeout(30)
                        .MigrationsAssembly("AspCoreApi") // Explicitly specify the migrations assembly
                )
                .EnableSensitiveDataLogging(environment.IsDevelopment())
                .ConfigureWarnings(warnings =>
                    warnings.Log(
                        (RelationalEventId.ConnectionOpened, LogLevel.Information),
                        (RelationalEventId.ConnectionClosed, LogLevel.Information)))
                .LogTo(
                    (eventId, level) => level == LogLevel.Error ||
                                        (environment.IsDevelopment() && level == LogLevel.Information),
                    (message) => Log.Logger.Information(message.ToString()))
        );
    }

    private static void ConfigureFirebirdDatabase(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("FbKoneksi");

        if (string.IsNullOrEmpty(connectionString))
        {
            Log.Error("Firebird connection string is not configured");
            throw new InvalidOperationException("Firebird connection string is not configured.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseFirebird(connectionString)
                .EnableSensitiveDataLogging(environment.IsDevelopment())
                .LogTo(
                    message => Log.Logger.Information(message),
                    LogLevel.Information,
                    DbContextLoggerOptions.DefaultWithUtcTime)
        );
    }
}