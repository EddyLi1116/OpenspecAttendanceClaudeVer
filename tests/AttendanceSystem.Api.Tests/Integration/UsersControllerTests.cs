using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AttendanceSystem.Api.Contracts;
using AttendanceSystem.Domain.Entities;
using AttendanceSystem.Infrastructure.Persistence;
using AttendanceSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AttendanceSystem.Api.Tests.Integration;

public class UsersControllerTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory = new();

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();
    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<(HttpClient client, long adminId)> LoginAsFreshAdmin()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = true });
        long adminId;
        using (var scope = _factory.Services.CreateScope())
        {
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
            adminId = admin.Id;
        }

        var resp = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@test", password = "Password1!xxx" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<LoginBody>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return (client, adminId);
    }

    [Fact]
    public async Task DeactivatingLastAdmin_Returns409()
    {
        var (client, adminId) = await LoginAsFreshAdmin();
        var resp = await client.PostAsync($"/api/users/{adminId}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("CANNOT_DEACTIVATE_LAST_ADMIN", body);
    }

    [Fact]
    public async Task RemovingLastAdminRole_Returns409()
    {
        var (client, adminId) = await LoginAsFreshAdmin();
        var resp = await client.PutAsJsonAsync($"/api/users/{adminId}", new UpdateUserRequest(
            "Admin",
            DepartmentId: null,
            ManagerUserId: null,
            HireDate: null,
            RoleCodes: new[] { Role.Employee }));
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("CANNOT_REMOVE_LAST_ADMIN", body);
    }

    [Fact]
    public async Task AssigningSelfAsManager_Returns400()
    {
        var (client, adminId) = await LoginAsFreshAdmin();
        var resp = await client.PutAsJsonAsync($"/api/users/{adminId}", new UpdateUserRequest(
            "Admin",
            DepartmentId: null,
            ManagerUserId: adminId,
            HireDate: null,
            RoleCodes: new[] { Role.Admin }));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("INVALID_MANAGER_SELF", body);
    }

    [Fact]
    public async Task CyclicManager_Returns400()
    {
        var (client, adminId) = await LoginAsFreshAdmin();

        // Create B with A as manager, then try to set A's manager to B (cycle).
        var createB = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(
            "b@test", "B", null, adminId, null, new[] { Role.Employee }));
        createB.EnsureSuccessStatusCode();
        var b = await createB.Content.ReadFromJsonAsync<UserListItem>();

        var cycleResp = await client.PutAsJsonAsync($"/api/users/{adminId}", new UpdateUserRequest(
            "Admin", null, b!.Id, null, new[] { Role.Admin }));
        Assert.Equal(HttpStatusCode.BadRequest, cycleResp.StatusCode);
        Assert.Contains("INVALID_MANAGER_CYCLE", await cycleResp.Content.ReadAsStringAsync());
    }

    private record LoginBody(string AccessToken, string TokenType, int ExpiresInSeconds, bool MustChangePassword);
}
