using FluentValidation;

namespace Worknexa.Application.Validation;

public static class UrlRules
{
    /// <summary>Optional links are rendered as anchors, so only absolute http(s) URLs are accepted (no javascript: or data:).</summary>
    public static IRuleBuilderOptions<T, string?> WebUrl<T>(this IRuleBuilder<T, string?> rule)
    {
        return rule
            .Must(value => string.IsNullOrWhiteSpace(value) || IsWebUrl(value))
            .WithMessage("Enter a full link that starts with http:// or https://.");
    }

    public static bool IsWebUrl(string value)
    {
        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}
