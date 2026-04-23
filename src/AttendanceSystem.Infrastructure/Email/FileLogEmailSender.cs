using System.Globalization;
using System.Text;
using AttendanceSystem.Domain.Email;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AttendanceSystem.Infrastructure.Email;

public class FileLogEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly IHostEnvironment _env;

    public FileLogEmailSender(IOptions<EmailOptions> options, IHostEnvironment env)
    {
        _options = options.Value;
        _env = env;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var outboxPath = Path.IsPathRooted(_options.OutboxDirectory)
            ? _options.OutboxDirectory
            : Path.Combine(_env.ContentRootPath, _options.OutboxDirectory);

        Directory.CreateDirectory(outboxPath);

        var now = DateTime.UtcNow;
        var safeTo = string.Concat(message.To.Split(Path.GetInvalidFileNameChars()));
        var filename = $"{now:yyyyMMdd-HHmmss-fff}-{safeTo}.eml";
        var fullPath = Path.Combine(outboxPath, filename);

        var boundary = $"boundary-{Guid.NewGuid():N}";
        var sb = new StringBuilder();
        sb.Append("From: ").Append(_options.FromAddress).Append("\r\n");
        sb.Append("To: ").Append(message.To).Append("\r\n");
        sb.Append("Subject: ").Append(EncodeSubject(message.Subject)).Append("\r\n");
        sb.Append("Date: ").Append(now.ToString("r", CultureInfo.InvariantCulture)).Append("\r\n");
        sb.Append("MIME-Version: 1.0\r\n");
        sb.Append("Content-Type: multipart/alternative; boundary=\"").Append(boundary).Append("\"\r\n");
        sb.Append("\r\n");
        sb.Append("This is a multi-part message in MIME format.\r\n");

        sb.Append("--").Append(boundary).Append("\r\n");
        sb.Append("Content-Type: text/plain; charset=utf-8\r\n");
        sb.Append("Content-Transfer-Encoding: 8bit\r\n\r\n");
        sb.Append(message.TextBody).Append("\r\n");

        sb.Append("--").Append(boundary).Append("\r\n");
        sb.Append("Content-Type: text/html; charset=utf-8\r\n");
        sb.Append("Content-Transfer-Encoding: 8bit\r\n\r\n");
        sb.Append(message.HtmlBody).Append("\r\n");

        sb.Append("--").Append(boundary).Append("--\r\n");

        await File.WriteAllTextAsync(fullPath, sb.ToString(), new UTF8Encoding(false), cancellationToken);
    }

    private static string EncodeSubject(string subject)
    {
        var bytes = Encoding.UTF8.GetBytes(subject);
        return "=?utf-8?B?" + Convert.ToBase64String(bytes) + "?=";
    }
}
