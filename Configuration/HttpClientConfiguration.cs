using AspCoreApi.Handlers;

namespace AspCoreApi.Configuration;

public static class HttpClientConfiguration
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        // Register the logging handler
        services.AddTransient<LoggingHttpMessageHandler>();

        // Add a named HTTP client with the logging handler
        services.AddHttpClient("ApiClient")
            .AddHttpMessageHandler<LoggingHttpMessageHandler>();

        // Add a typed HTTP client for specific API service
        services.AddHttpClient<IExternalApiService, ExternalApiService>(client =>
        {
            client.BaseAddress = new Uri("https://api.example.com/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<LoggingHttpMessageHandler>();

        // Note: To add Polly policies, you would need to add the Microsoft.Extensions.Http.Polly package
        // And then add: using Polly; using Polly.Extensions.Http; using Microsoft.Extensions.Http;
        // Then you could uncomment the following line:
        // .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    /* 
    // Uncomment this method if you add the Polly package
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
    */
}

// Interface and implementation for typed HTTP client
public interface IExternalApiService
{
    Task<T> GetAsync<T>(string endpoint);
}

public class ExternalApiService : IExternalApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiService> _logger;

    public ExternalApiService(HttpClient httpClient, ILogger<ExternalApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(string endpoint)
    {
        _logger.LogInformation("Making request to {Endpoint}", endpoint);

        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>()
               ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}