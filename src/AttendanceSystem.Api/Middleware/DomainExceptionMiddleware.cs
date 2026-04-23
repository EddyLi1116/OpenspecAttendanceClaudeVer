using System.Text.Json;
using AttendanceSystem.Domain.Exceptions;

namespace AttendanceSystem.Api.Middleware;

public class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainExceptionMiddleware> _logger;

    public DomainExceptionMiddleware(RequestDelegate next, ILogger<DomainExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (DomainException ex)
        {
            _logger.LogInformation("Domain exception: {Code} {Message}", ex.ErrorCode, ex.Message);
            ctx.Response.StatusCode = ex.HttpStatusCode;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            object body = ex is InvalidPasswordPolicyException pp
                ? new { errorCode = ex.ErrorCode, message = ex.Message, violations = pp.Violations }
                : new { errorCode = ex.ErrorCode, message = ex.Message };
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(body));
        }
    }
}
