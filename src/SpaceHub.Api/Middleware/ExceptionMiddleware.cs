using System.Text.Json;
using FluentValidation;
using SpaceHub.Api.DTOs;
using SpaceHub.Application.Exceptions;
namespace SpaceHub.Api.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionMiddleware> logger;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var response = ex switch
            {
                ValidationException validation => new ErrorResponse(400, "Validation failed",
                    validation.Errors.GroupBy(x => JsonNamingPolicy.CamelCase.ConvertName(x.PropertyName))
                        .ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).Distinct().ToArray())),
                BusinessException business => new ErrorResponse(business.Kind switch
                {
                    FailureKind.Validation => 400,
                    FailureKind.Unauthorized => 401,
                    FailureKind.Forbidden => 403,
                    FailureKind.NotFound => 404,
                    FailureKind.Conflict => 409,
                    _ => 500
                }, business.Message, business.Errors),
                _ => ErrorResponse.Create(500, "An unexpected error occurred.")
            };
            if (response.Status == 500)
                logger.LogError(ex, "Unhandled request error");
            context.Response.StatusCode = response.Status;
            if (response.Status == 401)
                context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
        }
    }
}
