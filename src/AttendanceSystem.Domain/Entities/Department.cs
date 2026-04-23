namespace AttendanceSystem.Domain.Entities;

public class Department
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<User> Members { get; set; } = new List<User>();
}
