using AttendanceSystem.Domain.Email;

namespace AttendanceSystem.Infrastructure.Email;

public class NullEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
}
