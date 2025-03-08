
using AspCoreApi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Plainquire.Filter.Mvc;
using Plainquire.Filter.Swashbuckle;

namespace AspCoreApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer(); // Required for minimal APIs

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Api BoilerPlate V1", Version = "v1" });
                options.AddFilterSupport();

                // Add JWT Authentication Support in Swagger
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Enter 'Bearer {token}'",
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
            var koneksi = builder.Configuration.GetConnectionString("FbKoneksi");
            builder.Services.AddDbContext<ApplicationDbContext>(
                options => options.UseFirebird(koneksi));
            builder.Services.AddIdentityApiEndpoints<IdentityUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;
            });
            
            builder.Services.AddControllers().AddFilterSupport();
            builder.Services.AddSwaggerGen(options => options.AddFilterSupport());
            //   builder.Services.AddTransient<IEmailSender, EmailSender>();


            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api BoilerPlate V1");
                    c.RoutePrefix = string.Empty; // Set Swagger UI at the root URL (optional)
               
                });

            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapGroup("/api")
               .MapIdentityApi<IdentityUser>().RequireAuthorization();
            //  app.MapSwagger().RequireAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
