namespace AttendanceSystem.Domain.Security;

public interface ICurrentUser
{
    long? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string roleCode);
    IReadOnlyList<string> Roles { get; }
    bool MustChangePassword { get; }
}
