namespace Worknexa.Api.Auth;

public sealed class AuthSettings
{
    public const string SectionName = "Auth";

    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "worknexa";

    public string Audience { get; set; } = "worknexa";

    public int TokenHours { get; set; } = 8;
}
