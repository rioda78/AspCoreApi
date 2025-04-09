namespace AspCoreApi.Ekstensi
{
    // Move configuration methods to separate static extension classes
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // Move service configuration here
            return services;
        }
    }
}
