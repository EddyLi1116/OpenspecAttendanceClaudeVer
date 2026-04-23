namespace AttendanceSystem.Api.Contracts;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresInSeconds,
    bool MustChangePassword,
    UserSummary User);

public record UserSummary(
    long Id,
    string Email,
    string DisplayName,
    long? DepartmentId,
    long? ManagerUserId,
    DateTime? HireDate,
    string EmploymentStatus,
    IReadOnlyList<string> RoleCodes);

public record ChangePasswordRequest(string OldPassword, string NewPassword);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword);
