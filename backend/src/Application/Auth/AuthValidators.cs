using FluentValidation;

namespace Worknexa.Application.Auth;

public static class PasswordRules
{
    /// <summary>Caps hashing work per request; long inputs make password hashing an easy CPU drain.</summary>
    public const int MaximumLength = 128;
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(PasswordRules.MaximumLength);
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(request => request.DisplayName).NotEmpty().MaximumLength(200);
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword).NotEmpty().MaximumLength(PasswordRules.MaximumLength);
        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(PasswordRules.MaximumLength)
            .Matches("[A-Za-z]").WithMessage("Password must include a letter.")
            .Matches("[0-9]").WithMessage("Password must include a number.")
            .NotEqual(request => request.CurrentPassword).WithMessage("Choose a different password.");
    }
}
