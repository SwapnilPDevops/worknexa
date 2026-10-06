namespace Worknexa.Application.Auth;

public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException()
        : base("Email or password is incorrect.")
    {
    }
}
