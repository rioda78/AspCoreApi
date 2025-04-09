using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace AspCoreApi.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        Log.Error(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var code = HttpStatusCode.InternalServerError;
        var result = string.Empty;

        switch (exception)
        {
            case KeyNotFoundException:
                code = HttpStatusCode.NotFound;
                break;
            case UnauthorizedAccessException:
                code = HttpStatusCode.Unauthorized;
                break;
            case ArgumentException:
            case InvalidOperationException:
                code = HttpStatusCode.BadRequest;
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        if (_environment.IsDevelopment())
        {
            // In development, return detailed error information
            var response = new
            {
                status = (int)code,
                error = exception.Message,
                detail = exception.StackTrace,
                innerError = exception.InnerException?.Message
            };

            result = JsonSerializer.Serialize(response);
        }
        else
        {
            // In production, return minimal error information
            var response = new
            {
                status = (int)code,
                error = "An error occurred while processing your request.",
                correlationId = context.TraceIdentifier
            };

            result = JsonSerializer.Serialize(response);
        }

        return context.Response.WriteAsync(result);
    }
}