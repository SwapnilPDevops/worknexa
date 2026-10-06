using Microsoft.Extensions.Diagnostics.HealthChecks;
using Worknexa.Api.Contracts;

namespace Worknexa.Api.Health;

public static class HealthResults
{
    public static IResult FromReport(HealthReport report)
    {
        if (report.Status == HealthStatus.Healthy)
        {
            return Results.Ok(new { status = "ok", version = Hosting.ProductionHosting.Version });
        }

        return Results.Json(
            new ApiError
            {
                Code = "UNHEALTHY",
                Message = "The service is unavailable",
                Errors = report.Entries
                    .Select(entry => new ApiFieldError
                    {
                        Field = entry.Key,
                        Message = string.IsNullOrWhiteSpace(entry.Value.Description)
                            ? entry.Value.Status.ToString()
                            : entry.Value.Description
                    })
                    .ToArray()
            },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
