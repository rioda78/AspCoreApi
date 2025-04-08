using System.Threading.RateLimiting;
using AspCoreApi.Data;
using AspCoreApi.Ekstensi;
using AspCoreApi.Models;
using AspCoreApi.Seeder.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace AspCoreApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog first, before any services are built
        ConfigureLogging();

        try
        {
            Log.Information("Starting web application");

            var builder = WebApplication.CreateBuilder(args);

            // Add Serilog to the application
            builder.Host.UseSerilog();

            // Add services to the container
            ConfigureServices(builder);

         
             var app = builder.Build();
                                           
                ConfigureMiddleware(app);

                // Seed the database
                await SeedDatabase(app);

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

    private static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code)
            .WriteTo.File(
                path: "logs/api-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}",
                retainedFileCountLimit: 31)
            .CreateLogger();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        // Add logging for EF Core
        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddSerilog(dispose: false);
        });

        builder.Services.AddControllers();

        // Configure rate limiting
        ConfigureRateLimiting(builder);

        // Configure Swagger and OpenAPI
        ConfigureSwagger(builder);

        // Configure database
        ConfigureDatabase(builder);

        // Configure Identity
        ConfigureIdentity(builder);

        // Add authorization
        builder.Services.AddAuthorization();

    

        builder.Services.AddTransient<LoggingHttpMessageHandler>();
        builder.Services.AddHttpClient("ApiClient")
            .AddHttpMessageHandler<LoggingHttpMessageHandler>();


        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
        builder.Services.AddHealthChecks();

        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = builder.Environment.IsDevelopment();
            options.ValidateOnBuild = true;
        });

        builder.Services.AddProblemDetails();


    }

    private static void ConfigureRateLimiting(WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(policyName: "fixed", options =>
            {
                options.PermitLimit = 4;
                options.Window = TimeSpan.FromSeconds(12);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 2;
            });

            // Add a global rate limiting policy
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter("GlobalLimit", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1)
                });
            });

            // Add event handlers for rate limiting events
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var logMessage = $"Rate limit exceeded for IP: {context.HttpContext.Connection.RemoteIpAddress}";
                Log.Warning(logMessage);

                await context.HttpContext.Response.WriteAsJsonAsync(new { error = "Too many requests. Please try again later." }, token);
            };
        });
    }

    private static void ConfigureSwagger(WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Api BoilerPlate",
                Version = "v1",
                Description = "ASP.NET Core API Boilerplate",
                Contact = new OpenApiContact
                {
                    Name = "API Support",
                    Email = "support@example.com"
                }
            });


            // Add JWT Authentication Support in Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    private static void ConfigureDatabase(WebApplicationBuilder builder)
    {
        var rdms = builder.Configuration.GetValue<string>("rdms")?.ToLowerInvariant();

        if (string.IsNullOrEmpty(rdms))
        {
            Log.Warning("Database type (rdms) is not specified in configuration, using default.");
            rdms = "mysql"; // Default to MySQL if not specified
        }

        Log.Information("Configuring database connection for {DatabaseType}", rdms);

        switch (rdms)
        {
            case "mysql":
                var mysqlConnection = builder.Configuration.GetConnectionString("MysqlKoneksi");

                if (string.IsNullOrEmpty(mysqlConnection))
                {
                    Log.Error("MySQL connection string is not configured");
                    throw new InvalidOperationException("MySQL connection string is not configured.");
                }

                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options
                        .UseMySql(
                            mysqlConnection,
                            ServerVersion.AutoDetect(mysqlConnection),
                            sqlOptions => sqlOptions
                                .EnableRetryOnFailure(
                                    maxRetryCount: 5,
                                    maxRetryDelay: TimeSpan.FromSeconds(30),
                                    errorNumbersToAdd: null)
                                .CommandTimeout(30)
                        )
                        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
                        .LogTo(
                            (eventId, level) => level == LogLevel.Error ||
                                               (builder.Environment.IsDevelopment() && level == LogLevel.Information),
                            (message) => Log.Logger.Information(message.ToString()))
                );
                break;

            case "firebird":
                var firebirdConnection = builder.Configuration.GetConnectionString("FbKoneksi");

                if (string.IsNullOrEmpty(firebirdConnection))
                {
                    Log.Error("Firebird connection string is not configured");
                    throw new InvalidOperationException("Firebird connection string is not configured.");
                }

                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options
                        .UseFirebird(firebirdConnection)
                        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
                        .LogTo(
                            message => Log.Logger.Information(message),
                            LogLevel.Information,
                            DbContextLoggerOptions.DefaultWithUtcTime)
                );
                break;

            default:
                Log.Error("Unsupported database type: {DatabaseType}", rdms);
                throw new InvalidOperationException($"Unsupported database type: {rdms}");
        }
    }

    private static void ConfigureIdentity(WebApplicationBuilder builder)
    {
        builder.Services
            .AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.Configure<IdentityOptions>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;
        });

        // Add auditing for identity operations
        builder.Services.AddScoped<IAuditableIdentityContext, AuditableIdentityContext>();
        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, DummyEmailSender<ApplicationUser>>();

    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        // Configure the HTTP request pipeline based on environment
        if (app.Environment.IsDevelopment())
        {
            Log.Information("Configuring for Development environment");
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api BoilerPlate V1");
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
            app.UseExceptionHandler();
            app.UseHsts();
        }

        // Add request logging middleware
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
            };
        });

        // Apply middleware in the correct order
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCors("AllowAll");
        app.MapHealthChecks("/health");
        

        // Map endpoints
        app.MapGroup("/api")
            .MapIdentityApi<ApplicationUser>()
            .RequireAuthorization();

        app.MapControllers();
    }

    private static async Task SeedDatabase(WebApplication app)
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

// HTTP logging handler for logging HTTP client requests
public class LoggingHttpMessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Log.Debug("HTTP Request: {Method} {Uri}", request.Method, request.RequestUri);

        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync(cancellationToken);
            Log.Debug("Request Content: {Content}", content);
        }

        var response = await base.SendAsync(request, cancellationToken);

        Log.Debug("HTTP Response: {StatusCode} for {Method} {Uri}",
            (int)response.StatusCode, request.Method, request.RequestUri);

        return response;
    }
}

// Auditing interface for identity operations
public interface IAuditableIdentityContext
{
    Task LogIdentityEvent(string eventName, string userId, string details);
}

// Implementation for auditing identity operations
public class AuditableIdentityContext : IAuditableIdentityContext
{
    private readonly ILogger<AuditableIdentityContext> _logger;

    public AuditableIdentityContext(ILogger<AuditableIdentityContext> logger)
    {
        _logger = logger;
    }

    public Task LogIdentityEvent(string eventName, string userId, string details)
    {
        _logger.LogInformation("Identity Event: {EventName} for User: {UserId} - Details: {Details}",
            eventName, userId, details);

        return Task.CompletedTask;
    }
}