using System.Reflection;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentDemo.Infrastructure.Data;
using PaymentDemo.Infrastructure.Http.Exceptions;
using PaymentDemo.Shared.Responses;
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