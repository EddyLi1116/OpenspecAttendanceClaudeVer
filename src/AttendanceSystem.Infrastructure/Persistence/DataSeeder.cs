using AttendanceSystem.Domain.Entities;
using AttendanceSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AttendanceSystem.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedInitialAdminAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AttendanceDbContext>>();

        var email = Environment.GetEnvironmentVariable("ATTENDANCE_ADMIN_EMAIL")
                    ?? config["Admin:SeedEmail"];
        var password = Environment.GetEnvironmentVariable("ATTENDANCE_ADMIN_INITIAL_PASSWORD")
                       ?? config["Admin:SeedInitialPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Admin seed skipped: ATTENDANCE_ADMIN_EMAIL or ATTENDANCE_ADMIN_INITIAL_PASSWORD not set. No admin account will exist.");
            return;
        }

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            logger.LogInformation("Admin seed skipped: user '{Email}' already exists.", email);
            return;
        }

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == Role.Admin, cancellationToken)
                        ?? throw new InvalidOperationException("Admin role not seeded. Run migrations first.");

        var now = DateTime.UtcNow;
        var user = new User
        {
            Email = email,
            DisplayName = "系統管理員",
            PasswordHash = hasher.Hash(password),
            MustChangePassword = true,
            EmploymentStatus = EmploymentStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded initial admin user '{Email}'.", email);
    }
}
