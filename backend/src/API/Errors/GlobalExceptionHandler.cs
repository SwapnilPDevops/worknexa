using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Worknexa.Api.Contracts;
using Worknexa.Application.Assistant;
using Worknexa.Application.Auth;
using Worknexa.Application.Common;
using Worknexa.Application.Integrations;

namespace Worknexa.Api.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, error) = Map(exception);
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            // Expected client errors: the exception type is enough, and messages can echo request input.
            _logger.LogWarning("Request failed with {ErrorCode} ({ExceptionType})", error.Code, exception.GetType().Name);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ApiError
        {
            Code = error.Code,
            Message = error.Message,
            Errors = error.Errors,
            TraceId = httpContext.TraceIdentifier
        }, cancellationToken);
        return true;
    }

    private static (int StatusCode, ApiError Error) Map(Exception exception)
    {
        return exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                new ApiError
                {
                    Code = "VALIDATION_ERROR",
                    Message = "Validation failed",
                    Errors = validation.Errors
                        .Select(error => new ApiFieldError
                        {
                            Field = error.PropertyName,
                            Message = error.ErrorMessage
                        })
                        .ToArray()
                }),
            InvalidSortException sort => (
                StatusCodes.Status400BadRequest,
                new ApiError
                {
                    Code = "VALIDATION_ERROR",
                    Message = "Validation failed",
                    Errors =
                    [
                        new ApiFieldError
                        {
                            Field = "sortBy",
                            Message = sort.Message
                        }
                    ]
                }),
            BadHttpRequestException badRequest => (
                badRequest.StatusCode,
                new ApiError
                {
                    Code = "BAD_REQUEST",
                    Message = "The request could not be read. Check the body and parameters."
                }),
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                new ApiError
                {
                    Code = "NOT_FOUND",
                    Message = notFound.Message
                }),
            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                new ApiError
                {
                    Code = "CONFLICT",
                    Message = conflict.Message
                }),
            AuthenticationFailedException failed => (
                StatusCodes.Status401Unauthorized,
                new ApiError
                {
                    Code = "UNAUTHORIZED",
                    Message = failed.Message
                }),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                new ApiError
                {
                    Code = "UNAUTHORIZED",
                    Message = "Authentication is required"
                }),
            ForbiddenException forbidden => (
                StatusCodes.Status403Forbidden,
                new ApiError
                {
                    Code = "FORBIDDEN",
                    Message = forbidden.Message
                }),
            AssistantUnavailableException unavailable => (
                StatusCodes.Status503ServiceUnavailable,
                new ApiError
                {
                    Code = "ASSISTANT_UNAVAILABLE",
                    Message = unavailable.Message
                }),
            IntegrationException integration => integration.Failure switch
            {
                IntegrationFailure.Auth => (
                    StatusCodes.Status409Conflict,
                    new ApiError { Code = "INTEGRATION_RECONNECT", Message = "Access to the connected account was revoked or expired. Reconnect to continue." }),
                IntegrationFailure.Transient => (
                    StatusCodes.Status503ServiceUnavailable,
                    new ApiError { Code = "INTEGRATION_UNAVAILABLE", Message = integration.Message }),
                IntegrationFailure.NotFound => (
                    StatusCodes.Status404NotFound,
                    new ApiError { Code = "NOT_FOUND", Message = integration.Message }),
                _ => (
                    StatusCodes.Status502BadGateway,
                    new ApiError { Code = "INTEGRATION_ERROR", Message = integration.Message })
            },
            _ => (
                StatusCodes.Status500InternalServerError,
                new ApiError
                {
                    Code = "UNEXPECTED_ERROR",
                    Message = "An unexpected error occurred"
                })
        };
    }
}
