using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UberGenius.Api.Data;

namespace UberGenius.Api.Auth;

public class JwtTokenService(IConfiguration configuration)
{
    public string CreateToken(User user)
    {
        var signingKey = configuration["Auth:JwtSigningKey"]
            ?? throw new InvalidOperationException("Auth:JwtSigningKey is not configured.");
        var issuer = configuration["Auth:JwtIssuer"] ?? "UberGenius";
        var audience = configuration["Auth:JwtAudience"] ?? "UberGenius";
        var expiryDays = configuration.GetValue("Auth:JwtExpiryDays", 7);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("displayName", user.DisplayName),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiryDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
