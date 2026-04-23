using AttendanceSystem.Api.Contracts;
using AttendanceSystem.Api.Services;
using AttendanceSystem.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "refresh_token";
    private const string RefreshCookiePath = "/api/auth";

    private readonly AuthService _auth;
    private readonly ICurrentUser _currentUser;

    public AuthController(AuthService auth, ICurrentUser currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var (response, refreshRaw, refreshExp) = await _auth.LoginAsync(req, ct);
        SetRefreshCookie(refreshRaw, refreshExp);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var raw) || string.IsNullOrEmpty(raw))
            return Unauthorized(new { errorCode = "INVALID_REFRESH_TOKEN", message = "Refresh token 缺失" });

        var (response, refreshRaw, refreshExp) = await _auth.RefreshAsync(raw, ct);
        SetRefreshCookie(refreshRaw, refreshExp);
        return Ok(response);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        Request.Cookies.TryGetValue(RefreshCookieName, out var raw);
        await _auth.LogoutAsync(raw, ct);
        ClearRefreshCookie();
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        await _auth.ChangePasswordAsync(userId, req, ct);
        ClearRefreshCookie();
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(req, ct);
        return StatusCode(StatusCodes.Status202Accepted, new { message = "若帳號存在，系統已寄出重設連結" });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        await _auth.ResetPasswordAsync(req, ct);
        return NoContent();
    }

    private void SetRefreshCookie(string rawToken, DateTime expiresAtUtc)
    {
        Response.Cookies.Append(RefreshCookieName, rawToken, new CookieOptions
        {
            HttpOnly = true,
            // Secure only over HTTPS so dev/test (http) clients can still round-trip the cookie.
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = new DateTimeOffset(expiresAtUtc, TimeSpan.Zero)
        });
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Append(RefreshCookieName, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = DateTimeOffset.UnixEpoch,
            MaxAge = TimeSpan.Zero
        });
    }
}
