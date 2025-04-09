using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

namespace AspCoreApi.Configuration;

public static class RateLimitingConfiguration
{
    public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Add a named policy for specific endpoints
            options.AddFixedWindowLimiter(policyName: "fixed", options =>
            {
                options.PermitLimit = 4;
                options.Window = TimeSpan.FromSeconds(12);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 2;
            });

            // Add another policy based on client IP
            options.AddPolicy("PerUserRateLimit", context =>
            {
                // Get user identifier or fallback to IP address
                var userId = context.User.Identity?.IsAuthenticated == true
                    ? context.User.FindFirst("sub")?.Value
                    : context.Connection.RemoteIpAddress?.ToString();

                return RateLimitPartition.GetTokenBucketLimiter(userId ?? "anonymous", _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 20,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    TokensPerPeriod = 5,
                    AutoReplenishment = true
                });
            });

            // Add a global rate limiting policy
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter("GlobalLimit", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                });
            });

            // Add event handlers for rate limiting events
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var logMessage = $"Rate limit exceeded for IP: {context.HttpContext.Connection.RemoteIpAddress}, Path: {context.HttpContext.Request.Path}";
                Log.Warning(logMessage);

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        error = "Too many requests. Please try again later.",
                        retryAfter = 10 // Suggested retry time in seconds
                    },
                    token);
            };
        });

        return services;
    }
}