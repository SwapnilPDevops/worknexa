using Worknexa.Application.Auth;
using Worknexa.Application.Projects;
using Worknexa.Application.Validation;
using Worknexa.Application.Work;

namespace Worknexa.Api.Tests;

public sealed class ValidatorTests
{
    [Theory]
    [InlineData("https://github.com/worknexa/app", true)]
    [InlineData("http://intranet.local/docs", true)]
    [InlineData("  https://example.com  ", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("JavaScript:alert(1)", false)]
    [InlineData("data:text/html,<script>alert(1)</script>", false)]
    [InlineData("file:///etc/passwd", false)]
    [InlineData("ftp://example.com/file", false)]
    [InlineData("github.com/worknexa", false)]
    [InlineData("/relative", false)]
    public void UrlRules_AcceptOnlyAbsoluteWebLinks(string value, bool expected)
    {
        Assert.Equal(expected, UrlRules.IsWebUrl(value));
    }

    [Fact]
    public void ProjectValidator_AllowsEmptyLinksAndRejectsScriptLinks()
    {
        var validator = new ProjectWriteValidator();
        var valid = Project(repositoryUrl: null, documentationUrl: "");
        var invalid = Project(repositoryUrl: "javascript:alert(1)", documentationUrl: "https://docs.example.com");

        Assert.True(validator.Validate(valid).IsValid);
        var result = validator.Validate(invalid);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ProjectWrite.RepositoryUrl));
    }

    [Theory]
    [InlineData("", "Todo", "High", false)]
    [InlineData("   ", "Todo", "High", false)]
    [InlineData("Write tests", "", "High", false)]
    [InlineData("Write tests", "Todo", "", false)]
    [InlineData("Write tests", "Todo", "High", true)]
    public void TaskValidator_RequiresTitleStatusAndPriority(string title, string status, string priority, bool expected)
    {
        var request = new TaskWriteRequest(title, null, status, priority, null, null, null, null, null, null, null, null, null);

        Assert.Equal(expected, new TaskWriteRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void TaskValidator_CapsLengthAndHours()
    {
        var request = new TaskWriteRequest(new string('t', 301), new string('d', 8001), "Todo", "High", null, null, null, null, null, null, -1, 10_001, null);

        var result = new TaskWriteRequestValidator().Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(TaskWriteRequest.Title));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(TaskWriteRequest.Description));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(TaskWriteRequest.EstimatedHours));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(TaskWriteRequest.ActualHours));
    }

    [Theory]
    [InlineData("admin@worknexa.local", "secret", true)]
    [InlineData("", "secret", false)]
    [InlineData("not-an-email", "secret", false)]
    [InlineData("admin@worknexa.local", "", false)]
    public void LoginValidator_RequiresEmailAndPassword(string email, string password, bool expected)
    {
        Assert.Equal(expected, new LoginRequestValidator().Validate(new LoginRequest(email, password)).IsValid);
    }

    [Fact]
    public void LoginValidator_CapsPasswordLength()
    {
        var request = new LoginRequest("admin@worknexa.local", new string('p', PasswordRules.MaximumLength + 1));

        Assert.False(new LoginRequestValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData("Current1", "Next-pass1", true)]
    [InlineData("Current1", "short1", false)]
    [InlineData("Current1", "nonumbers", false)]
    [InlineData("Current1", "12345678", false)]
    [InlineData("Current1", "Current1", false)]
    public void ChangePasswordValidator_EnforcesStrengthAndChange(string current, string next, bool expected)
    {
        Assert.Equal(expected, new ChangePasswordRequestValidator().Validate(new ChangePasswordRequest(current, next)).IsValid);
    }

    private static ProjectWrite Project(string? repositoryUrl, string? documentationUrl)
    {
        return new ProjectWrite("Atlas", "ATL", null, null, null, "Active", "High", null, null, null, repositoryUrl, documentationUrl);
    }
}
