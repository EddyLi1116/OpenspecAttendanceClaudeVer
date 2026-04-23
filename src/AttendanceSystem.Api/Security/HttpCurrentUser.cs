using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AttendanceSystem.Domain.Security;

namespace AttendanceSystem.Api.Security;

public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public long? UserId
    {
        get
        {
            var sub = Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                            ?? Principal?.FindFirst(ClaimTypes.Email)?.Value;

    public bool MustChangePassword =>
        string.Equals(Principal?.FindFirst("mustChangePassword")?.Value, "true", StringComparison.OrdinalIgnoreCase);

    public bool IsInRole(string roleCode) => Principal?.IsInRole(roleCode) == true;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();
}
