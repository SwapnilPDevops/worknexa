namespace Worknexa.Api.Contracts;

public sealed class ApiResponse<T>
{
    public required T Data { get; init; }
}

public sealed class ApiError
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public IReadOnlyList<ApiFieldError> Errors { get; init; } = [];

    /// <summary>Same value as the X-Correlation-ID header and the CorrelationId log property; quote it when reporting a problem.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; init; }
}

public sealed class ApiFieldError
{
    public required string Field { get; init; }

    public required string Message { get; init; }
}

public static class ApiResults
{
    public static IResult Success<T>(T data)
    {
        return Results.Ok(new ApiResponse<T> { Data = data });
    }
}
