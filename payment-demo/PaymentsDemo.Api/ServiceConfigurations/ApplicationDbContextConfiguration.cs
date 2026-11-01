using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PaymentsDemo.Api.Infrastructure.Data;

namespace PaymentsDemo.Api.ServiceConfigurations;

public static class ApplicationDbContextConfiguration
{
    public static IServiceCollection AddApplicationDbContextConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(config =>
        {
            config.UseNpgsql(configuration.GetConnectionString("my-app"),
            c => c.MigrationsAssembly(Assembly.GetExecutingAssembly()))
                  .UseSnakeCaseNamingConvention();
        });

        return services;
    }
}