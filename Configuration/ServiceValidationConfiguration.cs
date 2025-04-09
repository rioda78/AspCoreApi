namespace AspCoreApi.Configuration;

public static class ServiceValidationConfiguration
{
    public static void ConfigureServiceValidation(this WebApplicationBuilder builder)
    {
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = builder.Environment.IsDevelopment();
            options.ValidateOnBuild = true;
        });
    }
}