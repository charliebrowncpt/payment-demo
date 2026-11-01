using AcmePay.Features.Payments.SubmitPayment;
using AcmePay.Shared;

namespace AcmePay.Infrastructure;

// Authorization: Bearer <ACME_API_KEY>. Sandbox keys are prefixed sk_test_.
// Runs before model binding and validation, so a bad key is a 401 / E02 even on a bad body.
public static class ApiKeyFilter
{
    private const string Prefix = "Bearer sk_test_";

    public static ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var header = ctx.HttpContext.Request.Headers.Authorization.ToString();

        // TODO: replace the prefix check with a lookup of real keys.
        if (header.StartsWith(Prefix, StringComparison.Ordinal) && header.Length > Prefix.Length) return next(ctx);

        var body = PaymentResponseEnvelope.For(
                new PaymentResponse(
                    new ResponseDetail(
                        Code: "E02", 
                        Source: "Application", 
                        Description: "API key missing or invalid", 
                        ClientMessage: "Authentication failed."
                    )
                )
        );

        return ValueTask.FromResult<object?>(Results.Json(body, AcmeJson.Options, statusCode: StatusCodes.Status401Unauthorized));
    }
}