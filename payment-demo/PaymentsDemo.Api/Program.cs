using FastEndpoints;
using PaymentsDemo.Api.Infrastructure.HttpFilters;
using PaymentsDemo.Api.ServiceConfigurations;
using PaymentsDemo.Api.Shared.Responses;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer()
                .AddOpenApi()
                .AddFastEndpoints();

builder.Services.AddApplicationDbContextConfiguration(builder.Configuration)
                .AddExceptionHandler<ExceptionFilter>()
                .AddProblemDetails()
                .AddInfrastructureServices(builder.Configuration);

builder.Services.AddScoped<HateOasResponseBuilder>();


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