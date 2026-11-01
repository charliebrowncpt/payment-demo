using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PaymentDemo.Infrastructure.Exceptions;

namespace PaymentDemo.Infrastructure.Http.Exceptions;

public class ExceptionFilter : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is NotFoundException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

            await httpContext.Response.WriteAsJsonAsync(new
            {
                title = "Not Found",
                statusCode = 404,
                detail = exception.Message,
                path = httpContext.Request.Path.Value,
                traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier
            }, cancellationToken);
            return true;
        }
        return false;
    }
}