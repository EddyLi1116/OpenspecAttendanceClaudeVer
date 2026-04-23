using AttendanceSystem.Domain.Email;
using AttendanceSystem.Infrastructure.Email;
using AttendanceSystem.Infrastructure.Persistence;
using AttendanceSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AttendanceSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connString = config.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("ConnectionStrings:MySql missing");
        // Pinned MySQL 8.0 to keep design-time migrations deterministic without a live server.
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
        services.AddDbContext<AttendanceDbContext>(opts =>
            opts.UseMySql(connString, serverVersion));

        services.Configure<EmailOptions>(config.GetSection("Email"));

        var emailMode = config["Email:Mode"] ?? "FileLog";
        if (string.Equals(emailMode, "Null", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IEmailSender, NullEmailSender>();
        else
            services.AddScoped<IEmailSender, FileLogEmailSender>();

        services.AddSingleton<AttendanceSystem.Domain.Security.IPasswordPolicy, SystemPasswordPolicy>();
        services.AddSingleton<AttendanceSystem.Domain.Security.IInitialPasswordGenerator, CryptoInitialPasswordGenerator>();
        services.AddSingleton<IPasswordHasher, BcryptOrPbkdf2PasswordHasherAdapter>();

        services.Configure<JwtOptions>(config.GetSection("Jwt"));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
