namespace Worknexa.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record UpdateProfileRequest(string DisplayName);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
