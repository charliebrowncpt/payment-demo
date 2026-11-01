using System.Reflection;
using Amazon;
using Amazon.SQS;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentsDemo.Api.Api.Infrastructure.Configuration;
using PaymentsDemo.Api.Infrastructure.Data;
using PaymentsDemo.Api.Infrastructure.HttpFilters;
using PaymentsDemo.Api.Shared.Responses;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer()
                .AddOpenApi()
                .AddFastEndpoints();

builder.Services.AddDbContext<ApplicationDbContext>(config =>
{
    config.UseNpgsql(builder.Configuration.GetConnectionString("my-app"),
       c => c.MigrationsAssembly(Assembly.GetExecutingAssembly()))
          .UseSnakeCaseNamingConvention();
});

builder.Services.AddScoped<HateOasResponseBuilder>();

builder.Services.AddExceptionHandler<ExceptionFilter>()
                .AddProblemDetails();


//Config options
builder.Services.AddOptions<AcmePayServiceOptions>()
                .Bind(builder.Configuration.GetSection("AcmePaymentService"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

builder.Services.AddOptions<AwsConfig>()
                .Bind(builder.Configuration.GetSection("AWS"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

//AWS Configuration

builder.Services.AddScoped<IAmazonSQS>(sp =>
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

var app = builder.Build();

app.UseExceptionHandler();

app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";
    config.Versioning.Prefix = "v";
    config.Versioning.PrependToRoute = true;
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                 // serves /openapi/v1.json
    app.MapScalarApiReference();      // serves /scalar
}

app.Run();

public partial class Program { } // for integration tests