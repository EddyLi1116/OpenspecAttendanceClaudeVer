namespace AttendanceSystem.Domain.Entities;

public class Role
{
    public const string Admin = "admin";
    public const string Employee = "employee";

    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
