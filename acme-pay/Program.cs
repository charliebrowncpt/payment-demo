using System.Text.Json.Serialization;
using AcmePay.Features.Payments.SubmitPayment;
using AcmePay.Infrastructure;
using AcmePay.Shared;
using FastEndpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer()
                .AddOpenApi()
                .AddFastEndpoints()
                .AddAuthentication();

builder.Services.AddSingleton<InMemoryPaymentStore>();
builder.Services.AddScoped<IssuerSimulator>();

var app = builder.Build();

app.UseAuthentication();

app.MapFastEndpoints(config =>
{
   config.Versioning.Prefix = "v";
   config.Versioning.PrependToRoute = true;
   config.Versioning.DefaultVersion = int.Parse(builder.Configuration["Api:Version"]!.ToString());

    config.Serializer.Options.PropertyNamingPolicy = null;     // PascalCase on the wire
    config.Serializer.Options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

    // 400 / E01: the body could not be parsed, or a required field is missing.
    // Same envelope as every other response, with the reason in ResponseDetail.
    config.Errors.ResponseBuilder = (failures, ctx, statusCode) =>
        PaymentResponseEnvelope.For(new PaymentResponse(
            new ResponseDetail(
                Code: "E01", 
                Source: "Application", 
                Description: "Malformed request or missing field", 
                ClientMessage: "The request could not be processed."
            )
        ));
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.Run();
