namespace AttendanceSystem.Domain.Email;

public class EmailMessage
{
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public required string TextBody { get; init; }
}
