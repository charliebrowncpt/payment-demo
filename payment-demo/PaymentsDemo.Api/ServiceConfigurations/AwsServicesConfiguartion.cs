using Amazon;
using Amazon.SQS;
using Microsoft.Extensions.Options;
using PaymentsDemo.Api.Api.Infrastructure.Configuration;

namespace PaymentsDemo.Api.ServiceConfigurations;
public static class AwsServicesConfiguration
{
    public static IServiceCollection AddAwsServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AwsConfig>()
                .Bind(configuration.GetSection("AWS"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddScoped<IAmazonSQS>(sp =>
        {
            var awsConfig = sp.GetRequiredService<IOptions<AwsConfig>>().Value;

            var config = new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(awsConfig.Region)
            };

            if (awsConfig.UseLocalStack && !string.IsNullOrEmpty(awsConfig.ServiceUrl))
            {
                config.ServiceURL = awsConfig.ServiceUrl;
                config.UseHttp = true;
            }

            return new AmazonSQSClient(config);
        });

        return services;
    }
}