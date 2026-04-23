using System.Security.Cryptography;
using System.Text;
using AttendanceSystem.Api.Contracts;
using AttendanceSystem.Api.Templates;
using AttendanceSystem.Domain.Email;
using AttendanceSystem.Domain.Entities;
using AttendanceSystem.Domain.Exceptions;
using AttendanceSystem.Domain.Security;
using AttendanceSystem.Infrastructure.Persistence;
using AttendanceSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AttendanceSystem.Api.Services;

public class AuthService
{
    private readonly AttendanceDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IPasswordPolicy _policy;
    private readonly IJwtTokenService _jwt;
    private readonly IEmailSender _emailSender;
    private readonly JwtOptions _jwtOptions;
    private readonly IConfiguration _config;

    public AuthService(
        AttendanceDbContext db,
        IPasswordHasher hasher,
        IPasswordPolicy policy,
        IJwtTokenService jwt,
        IEmailSender emailSender,
        IOptions<JwtOptions> jwtOptions,
        IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _policy = policy;
        _jwt = jwt;
        _emailSender = emailSender;
        _jwtOptions = jwtOptions.Value;
        _config = config;
    }

    public async Task<(LoginResponse Response, string RefreshRaw, DateTime RefreshExpiresAt)> LoginAsync(
        LoginRequest req, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == req.Email, ct);

        if (user is null)
            throw new InvalidCredentialsException();

        var verify = _hasher.Verify(user.PasswordHash, req.Password);
        if (verify == PasswordVerificationOutcome.Failed)
            throw new InvalidCredentialsException();

        if (user.EmploymentStatus == EmploymentStatus.Inactive)
            throw new AccountInactiveException();

        if (verify == PasswordVerificationOutcome.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.Hash(req.Password);
            user.UpdatedAt = DateTime.UtcNow;
        }

        var roleCodes = user.UserRoles.Select(r => r.Role!.Code).ToArray();
        var access = _jwt.IssueAccessToken(user, roleCodes);
        var refresh = _jwt.IssueRefreshToken(user);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refresh.TokenHash,
            ExpiresAt = refresh.ExpiresAtUtc,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        var response = new LoginResponse(
            access.Token,
            "Bearer",
            access.ExpiresInSeconds,
            user.MustChangePassword,
            ToSummary(user, roleCodes));

        return (response, refresh.RawToken, refresh.ExpiresAtUtc);
    }

    public async Task<(LoginResponse Response, string RefreshRaw, DateTime RefreshExpiresAt)> RefreshAsync(
        string rawRefresh, CancellationToken ct)
    {
        var hash = _jwt.HashRefreshToken(rawRefresh);
        var token = await _db.RefreshTokens
            .Include(t => t.User)
                .ThenInclude(u => u!.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null)
            throw new InvalidRefreshTokenException();

        if (token.RevokedAt is not null)
        {
            // Replay detected: revoke every outstanding token for this user.
            await _db.RefreshTokens
                .Where(t => t.UserId == token.UserId && t.RevokedAt == null)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.RevokedAt, DateTime.UtcNow), ct);
            throw new InvalidRefreshTokenException();
        }

        if (token.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidRefreshTokenException();

        var user = token.User!;
        if (user.EmploymentStatus == EmploymentStatus.Inactive)
            throw new AccountInactiveException();

        token.RevokedAt = DateTime.UtcNow;

        var roleCodes = user.UserRoles.Select(r => r.Role!.Code).ToArray();
        var access = _jwt.IssueAccessToken(user, roleCodes);
        var refresh = _jwt.IssueRefreshToken(user);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refresh.TokenHash,
            ExpiresAt = refresh.ExpiresAtUtc,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        var response = new LoginResponse(
            access.Token,
            "Bearer",
            access.ExpiresInSeconds,
            user.MustChangePassword,
            ToSummary(user, roleCodes));
        return (response, refresh.RawToken, refresh.ExpiresAtUtc);
    }

    public async Task LogoutAsync(string? rawRefresh, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(rawRefresh))
            return;
        var hash = _jwt.HashRefreshToken(rawRefresh);
        await _db.RefreshTokens
            .Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.RevokedAt, DateTime.UtcNow), ct);
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new EntityNotFoundException("User", userId);

        if (_hasher.Verify(user.PasswordHash, req.OldPassword) == PasswordVerificationOutcome.Failed)
            throw new InvalidCredentialsException();

        var policy = _policy.Validate(req.NewPassword);
        if (!policy.IsValid)
            throw new InvalidPasswordPolicyException(policy.Violations);

        if (string.Equals(req.OldPassword, req.NewPassword, StringComparison.Ordinal))
            throw new PasswordSameAsOldException();

        user.PasswordHash = _hasher.Hash(req.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.RevokedAt, DateTime.UtcNow), ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email && u.EmploymentStatus == EmploymentStatus.Active, ct);
        if (user is null) return;

        var bytes = new byte[48];
        RandomNumberGenerator.Fill(bytes);
        var rawToken = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        var webBase = _config["AppUrls:WebBaseUrl"] ?? "http://localhost:5173";
        var resetLink = $"{webBase.TrimEnd('/')}/reset-password?token={rawToken}";
        var taipei = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Taipei Standard Time" : "Asia/Taipei");
        var expiresAtTaipei = TimeZoneInfo.ConvertTimeFromUtc(expiresAt, taipei).ToString("yyyy-MM-dd HH:mm");

        var message = ResetPasswordEmailTemplate.Build(user.DisplayName, resetLink, expiresAtTaipei, user.Email);
        await _emailSender.SendAsync(message, ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(req.Token)));
        var token = await _db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null || token.UsedAt is not null || token.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidResetTokenException();

        var policy = _policy.Validate(req.NewPassword);
        if (!policy.IsValid)
            throw new InvalidPasswordPolicyException(policy.Violations);

        var user = token.User!;
        user.PasswordHash = _hasher.Hash(req.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        token.UsedAt = DateTime.UtcNow;

        await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.RevokedAt, DateTime.UtcNow), ct);
        await _db.SaveChangesAsync(ct);
    }

    public static UserSummary ToSummary(User user, IEnumerable<string> roleCodes) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.DepartmentId,
        user.ManagerUserId,
        user.HireDate,
        user.EmploymentStatus.ToString().ToLowerInvariant(),
        roleCodes.ToArray());
}
