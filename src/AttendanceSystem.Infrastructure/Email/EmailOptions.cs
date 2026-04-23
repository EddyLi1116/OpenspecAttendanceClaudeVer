namespace AttendanceSystem.Infrastructure.Email;

public class EmailOptions
{
    public string Mode { get; set; } = "FileLog";
    public string OutboxDirectory { get; set; } = "App_Data/outbox";
    public string FromAddress { get; set; } = "no-reply@attendance.local";
}
