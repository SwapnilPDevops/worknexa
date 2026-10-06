using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Worknexa.Application.Abstractions;

namespace Worknexa.Api.Integrations;

/// <summary>Encrypts provider tokens and OAuth state with ASP.NET Core Data Protection.</summary>
public sealed class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("Worknexa.Integrations.v1");

    public string Protect(string value) => _protector.Protect(value);

    /// <summary>Returns null for tampered values or values protected with keys this server no longer has.</summary>
    public string? Unprotect(string protectedValue)
    {
        try
        {
            return _protector.Unprotect(protectedValue);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
