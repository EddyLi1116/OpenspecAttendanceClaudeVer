using AttendanceSystem.Api.Middleware;
using AttendanceSystem.Api.Security;
using AttendanceSystem.Api.Services;
using AttendanceSystem.Domain.Security;
using AttendanceSystem.Infrastructure;
using AttendanceSystem.Infrastructure.Persistence;
using AttendanceSystem.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Production safety gate: FileLog email sender is for development only.
// Spec: email-delivery "生產環境安全閘門".
if (builder.Environment.IsProduction() &&
    string.Equals(builder.Configuration["Email:Mode"], "FileLog", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("FileLogEmailSender 禁止在 Production 使用");
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<DepartmentService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Use legacy JwtSecurityTokenHandler which is permissive about missing 'kid' headers.
        options.UseSecurityTokenValidators = true;
        // Resolve configuration lazily so test-time overrides (WebApplicationFactory's
        // ConfigureAppConfiguration) take effect; capturing these values outside the
        // lambda at top-level would snapshot them before the test config is merged.
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var signingKey = jwtSection["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey missing");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration["AppUrls:WebBaseUrl"] ?? "http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<DomainExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<PasswordChangeRequiredMiddleware>();
app.UseAuthorization();
app.MapControllers();

await DataSeeder.SeedInitialAdminAsync(app.Services);

app.Run();

public partial class Program { }
