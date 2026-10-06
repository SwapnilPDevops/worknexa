using System.Security.Cryptography;
using System.Text;

namespace Worknexa.Application.Auth;

public static class PasswordMarkers
{
    public static string FromHash(string passwordHash)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash));
        return Convert.ToHexString(hash)[..16];
    }
}
