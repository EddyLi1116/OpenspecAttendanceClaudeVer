using AttendanceSystem.Domain.Email;
using AttendanceSystem.Domain.Entities;
using AttendanceSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AttendanceSystem.Api.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    public RecordingEmailSender EmailSender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MySql"] = "Server=127.0.0.1;Database=x;User=x;Password=x",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:SigningKey"] = "test-signing-key-with-at-least-thirty-two-bytes!!",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7",
                ["Email:Mode"] = "Null",
                ["Admin:SeedEmail"] = "",
                ["Admin:SeedInitialPassword"] = "",
                ["AppUrls:WebBaseUrl"] = "http://localhost:5173"
            });
        });

        builder.ConfigureServices(services =>
        {
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AttendanceDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(AttendanceDbContext) ||
                (d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true && d.ServiceType.Namespace != "Microsoft.EntityFrameworkCore.Diagnostics") ||
                (d.ServiceType.FullName?.Contains("Pomelo") == true)
            ).ToList();
            foreach (var d in toRemove) services.Remove(d);

            _connection.Open();
            services.AddDbContext<AttendanceDbContext>(opts => opts.UseSqlite(_connection));

            var emailDescriptor = services.Single(d => d.ServiceType == typeof(IEmailSender));
            services.Remove(emailDescriptor);
            services.AddSingleton<IEmailSender>(EmailSender);
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        await db.Database.EnsureCreatedAsync();
        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Role { Id = 1, Code = Role.Admin, Name = "系統管理員" },
                new Role { Id = 2, Code = Role.Employee, Name = "一般員工" });
            await db.SaveChangesAsync();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}

public class RecordingEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = new();
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        lock (Sent) Sent.Add(message);
        return Task.CompletedTask;
    }
}
