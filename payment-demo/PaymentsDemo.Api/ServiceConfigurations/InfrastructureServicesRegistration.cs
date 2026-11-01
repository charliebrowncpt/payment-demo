using PaymentsDemo.Api.Api.Infrastructure.Configuration;
using PaymentsDemo.Api.Infrastructure.Acme;
using PaymentsDemo.Api.Infrastructure.Services.AcmeService;

namespace PaymentsDemo.Api.ServiceConfigurations;
public static class InfrastructureServicesRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AcmePayServiceOptions>()
                .Bind(configuration.GetSection("AcmePaymentService"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddScoped<IAcmeQueueService, AcmeQueueService>();
        services.AddScoped<AcmeServiceEnvelopeBuilder>();

        //AWS registration
        services.AddAwsServicesConfiguration(configuration);

        return services;
    }
}