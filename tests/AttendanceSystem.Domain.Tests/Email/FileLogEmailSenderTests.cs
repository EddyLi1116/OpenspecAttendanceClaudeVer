using AttendanceSystem.Domain.Email;
using AttendanceSystem.Infrastructure.Email;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace AttendanceSystem.Domain.Tests.Email;

public class FileLogEmailSenderTests
{
    private sealed class FakeEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    [Fact]
    public async Task SendAsync_WritesEmlWithMultipartStructure()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "attendance-eml-" + Guid.NewGuid().ToString("N"));
        var env = new FakeEnv { ContentRootPath = tempRoot };
        var options = Options.Create(new EmailOptions { OutboxDirectory = "outbox", FromAddress = "from@test.local" });
        var sender = new FileLogEmailSender(options, env);

        var message = new EmailMessage
        {
            To = "user@test.local",
            Subject = "測試主旨",
            HtmlBody = "<p>html</p>",
            TextBody = "text"
        };

        await sender.SendAsync(message, CancellationToken.None);

        var outbox = Path.Combine(tempRoot, "outbox");
        Assert.True(Directory.Exists(outbox));
        var files = Directory.GetFiles(outbox, "*.eml");
        Assert.Single(files);

        var body = await File.ReadAllTextAsync(files[0]);
        Assert.Contains("To: user@test.local", body);
        Assert.Contains("From: from@test.local", body);
        Assert.Contains("Content-Type: multipart/alternative", body);
        Assert.Contains("Content-Type: text/plain", body);
        Assert.Contains("Content-Type: text/html", body);
        Assert.Contains("<p>html</p>", body);
        Assert.Contains("text", body);

        Directory.Delete(tempRoot, recursive: true);
    }

    [Fact]
    public async Task SendAsync_CreatesOutboxDirectoryIfMissing()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "attendance-eml-" + Guid.NewGuid().ToString("N"));
        var env = new FakeEnv { ContentRootPath = tempRoot };
        var options = Options.Create(new EmailOptions { OutboxDirectory = "missing/outbox", FromAddress = "from@test.local" });
        var sender = new FileLogEmailSender(options, env);

        await sender.SendAsync(new EmailMessage { To = "a@b", Subject = "s", HtmlBody = "h", TextBody = "t" }, CancellationToken.None);

        Assert.True(Directory.Exists(Path.Combine(tempRoot, "missing", "outbox")));
        Directory.Delete(tempRoot, recursive: true);
    }
}
