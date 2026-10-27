using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Abstractions;

namespace SkillHub.Infrastructure.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtTokenService(IConfiguration configuration, IDateTimeProvider dateTimeProvider)
    {
        _configuration = configuration;
        _dateTimeProvider = dateTimeProvider;
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateToken(
        string identityUserId,
        Guid profileId,
        string fullName,
        string email,
        string role)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secretKey = jwtSection["Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured.");
        var issuer = jwtSection["Issuer"] ?? "SkillHubAPI";
        var audience = jwtSection["Audience"] ?? "SkillHubClient";
        var expiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var parsed) ? parsed : 120;

        var nowUtc = _dateTimeProvider.UtcNow;
        var expiresAtUtc = nowUtc.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, identityUserId),
            new(ClaimTypes.NameIdentifier, identityUserId),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Role, role),
            new("profileId", profileId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        return (tokenString, expiresAtUtc);
    }
}
