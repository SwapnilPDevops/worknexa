using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Auth;

namespace Worknexa.Api.Auth;

public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAt);

public sealed class JwtTokenIssuer
{
    private readonly AuthSettings _settings;

    public JwtTokenIssuer(IOptions<AuthSettings> settings)
    {
        _settings = settings.Value;
    }

    public IssuedAccessToken Create(AccountRecord account)
    {
        if (account.PasswordHash is null)
        {
            throw new InvalidOperationException("Cannot issue a session without a password hash.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddHours(_settings.TokenHours);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Email, account.Email),
            new(ClaimTypes.Name, account.DisplayName),
            new("pwd", PasswordMarkers.FromHash(account.PasswordHash))
        };
        claims.AddRange(account.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new IssuedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
