using System.Text.Json;
using AttendanceSystem.Domain.Security;

namespace AttendanceSystem.Api.Middleware;

public class PasswordChangeRequiredMiddleware
{
    private static readonly HashSet<string> AllowedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/change-password",
        "/api/auth/logout",
        "/api/auth/refresh",
        "/api/auth/login",
        "/api/auth/forgot-password",
        "/api/auth/reset-password"
    };

    private readonly RequestDelegate _next;

    public PasswordChangeRequiredMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, ICurrentUser currentUser)
    {
        if (currentUser.IsAuthenticated && currentUser.MustChangePassword)
        {
            var path = ctx.Request.Path.Value ?? string.Empty;
            if (!AllowedPaths.Contains(path))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                ctx.Response.ContentType = "application/json; charset=utf-8";
                var body = JsonSerializer.Serialize(new
                {
                    errorCode = "PASSWORD_CHANGE_REQUIRED",
                    message = "請先完成首次密碼變更"
                });
                await ctx.Response.WriteAsync(body);
                return;
            }
        }
        await _next(ctx);
    }
}
