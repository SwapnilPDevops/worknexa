namespace Worknexa.Application.Abstractions;

/// <summary>Encrypts values that must never be stored or sent in plain text, such as provider tokens.</summary>
public interface ISecretProtector
{
    string Protect(string value);

    /// <summary>Returns null when the value was tampered with or the key is no longer available.</summary>
    string? Unprotect(string protectedValue);
}
