using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AttendanceSystem.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AttendanceSystem.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opts;
    private readonly SigningCredentials _credentials;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(IOptions<JwtOptions> opts)
    {
        _opts = opts.Value;
        var keyBytes = Encoding.UTF8.GetBytes(_opts.SigningKey);
        if (keyBytes.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes / 256 bits.");
        var key = new SymmetricSecurityKey(keyBytes);
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _opts.Issuer,
            ValidAudience = _opts.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };
    }

    public TokenValidationParameters ValidationParameters => _validationParameters;

    public AccessTokenResult IssueAccessToken(User user, IEnumerable<string> roleCodes)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_opts.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("displayName", user.DisplayName),
            new("mustChangePassword", user.MustChangePassword ? "true" : "false"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        foreach (var role in roleCodes)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: _credentials);

        var raw = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessTokenResult(raw, (int)(expiresAt - now).TotalSeconds, expiresAt);
    }

    public RefreshTokenResult IssueRefreshToken(User user)
    {
        var bytes = new byte[48];
        RandomNumberGenerator.Fill(bytes);
        var raw = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var hash = HashRefreshToken(raw);
        var expiresAt = DateTime.UtcNow.AddDays(_opts.RefreshTokenDays);
        return new RefreshTokenResult(raw, hash, expiresAt);
    }

    public string HashRefreshToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }

    public bool TryParseAccessToken(string token, out ClaimsPrincipal? principal)
    {
        principal = null;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            principal = handler.ValidateToken(token, _validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
