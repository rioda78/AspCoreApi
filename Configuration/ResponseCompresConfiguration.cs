namespace AspCoreApi.Configuration;

public static class ResponseCompresConfiguration
{
    public static IServiceCollection AddResponseKompres(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });
        return services;
    }
}