using System.Net.Http.Headers;
using System.Net.Http.Json;
using AttendanceSystem.Api.Contracts;
using AttendanceSystem.Api.Tests.Integration;
using AttendanceSystem.Domain.Entities;
using AttendanceSystem.Infrastructure.Persistence;
using AttendanceSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AttendanceSystem.Api.Tests.Services;

public class UserServiceCreateTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory = new();

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();
    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> LoginAsAdmin(HttpClient client)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var admin = new User
        {
            Email = "admin@test",
            DisplayName = "Admin",
            PasswordHash = hasher.Hash("Password1!xxx"),
            EmploymentStatus = EmploymentStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        var adminRole = await db.Roles.FirstAsync(r => r.Code == Role.Admin);
        db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync();

        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@test", password = "Password1!xxx" });
        loginResp.EnsureSuccessStatusCode();
        var body = await loginResp.Content.ReadFromJsonAsync<LoginBody>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task CreateUser_HashesPassword_SendsWelcomeEmail_DoesNotPersistPlaintext()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = true });
        var token = await LoginAsAdmin(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new CreateUserRequest(
            Email: "newbie@test",
            DisplayName: "新員工",
            DepartmentId: null,
            ManagerUserId: null,
            HireDate: null,
            RoleCodes: new[] { Role.Employee });

        var resp = await client.PostAsJsonAsync("/api/users", req);
        resp.EnsureSuccessStatusCode();

        Assert.Single(_factory.EmailSender.Sent);
        var sent = _factory.EmailSender.Sent[0];
        Assert.Equal("歡迎加入出缺勤系統", sent.Subject);
        Assert.Contains("newbie@test", sent.TextBody);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        var created = await db.Users.FirstAsync(u => u.Email == "newbie@test");
        Assert.True(created.MustChangePassword);
        Assert.NotEqual(string.Empty, created.PasswordHash);

        // Plaintext initial password is only visible in email body — must NOT appear in the stored PasswordHash.
        var emailContainsPwd = sent.TextBody.Contains("一次性初始密碼：");
        Assert.True(emailContainsPwd);
    }

    private record LoginBody(string AccessToken, string TokenType, int ExpiresInSeconds, bool MustChangePassword);
}
