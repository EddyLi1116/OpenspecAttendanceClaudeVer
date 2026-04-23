namespace AttendanceSystem.Api.Contracts;

public record CreateUserRequest(
    string Email,
    string DisplayName,
    long? DepartmentId,
    long? ManagerUserId,
    DateTime? HireDate,
    IReadOnlyList<string> RoleCodes);

public record UpdateUserRequest(
    string DisplayName,
    long? DepartmentId,
    long? ManagerUserId,
    DateTime? HireDate,
    IReadOnlyList<string> RoleCodes);

public record UserListItem(
    long Id,
    string Email,
    string DisplayName,
    long? DepartmentId,
    string? DepartmentName,
    long? ManagerUserId,
    string? ManagerDisplayName,
    DateTime? HireDate,
    string EmploymentStatus,
    IReadOnlyList<string> RoleCodes);

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
