using System.Security.Claims;
using AttendanceSystem.Domain.Entities;

namespace AttendanceSystem.Infrastructure.Security;

public interface IJwtTokenService
{
    AccessTokenResult IssueAccessToken(User user, IEnumerable<string> roleCodes);
    RefreshTokenResult IssueRefreshToken(User user);
    bool TryParseAccessToken(string token, out ClaimsPrincipal? principal);
    string HashRefreshToken(string rawToken);
}

public record AccessTokenResult(string Token, int ExpiresInSeconds, DateTime ExpiresAtUtc);
public record RefreshTokenResult(string RawToken, string TokenHash, DateTime ExpiresAtUtc);
