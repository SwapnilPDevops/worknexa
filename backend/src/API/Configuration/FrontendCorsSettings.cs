namespace Worknexa.Api.Configuration;

public sealed class FrontendCorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}
