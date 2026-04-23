namespace AttendanceSystem.Api.Contracts;

public record DepartmentDto(long Id, string Code, string Name);
public record CreateDepartmentRequest(string Code, string Name);
public record UpdateDepartmentRequest(string Code, string Name);
