using AttendanceSystem.Api.Contracts;
using AttendanceSystem.Api.Templates;
using AttendanceSystem.Domain.Email;
using AttendanceSystem.Domain.Entities;
using AttendanceSystem.Domain.Exceptions;
using AttendanceSystem.Domain.Security;
using AttendanceSystem.Infrastructure.Persistence;
using AttendanceSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Api.Services;

public class UserService
{
    private static readonly string[] AllowedRoles = new[] { Role.Admin, Role.Employee };
    private const int ManagerCycleDepthLimit = 10;

    private readonly AttendanceDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IInitialPasswordGenerator _passwordGen;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;

    public UserService(
        AttendanceDbContext db,
        IPasswordHasher hasher,
        IInitialPasswordGenerator passwordGen,
        IEmailSender emailSender,
        IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _passwordGen = passwordGen;
        _emailSender = emailSender;
        _config = config;
    }

    public async Task<UserListItem> CreateAsync(CreateUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            throw new ArgumentException("Email is required", nameof(req));

        if (await _db.Users.AnyAsync(u => u.Email == req.Email, ct))
            throw new EmailAlreadyExistsException(req.Email);

        ValidateRoleCodes(req.RoleCodes);
        await ValidateManagerAsync(null, req.ManagerUserId, ct);

        var initialPassword = _passwordGen.Generate();
        var now = DateTime.UtcNow;
        var user = new User
        {
            Email = req.Email,
            DisplayName = req.DisplayName,
            PasswordHash = _hasher.Hash(initialPassword),
            MustChangePassword = true,
            EmploymentStatus = EmploymentStatus.Active,
            DepartmentId = req.DepartmentId,
            ManagerUserId = req.ManagerUserId,
            HireDate = req.HireDate,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await AssignRolesAsync(user.Id, req.RoleCodes, ct);
        await _db.SaveChangesAsync(ct);

        var webBase = _config["AppUrls:WebBaseUrl"] ?? "http://localhost:5173";
        var loginUrl = $"{webBase.TrimEnd('/')}/login";
        var welcome = WelcomeEmailTemplate.Build(user.DisplayName, user.Email, loginUrl, initialPassword);
        await _emailSender.SendAsync(welcome, ct);

        return await LoadAsync(user.Id, ct);
    }

    public async Task<PagedResult<UserListItem>> ListAsync(int page, int pageSize, string? search, long? departmentId, string? status, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 500) pageSize = 500;

        var query = _db.Users
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Email.Contains(search) || u.DisplayName.Contains(search));
        if (departmentId.HasValue)
            query = query.Where(u => u.DepartmentId == departmentId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EmploymentStatus>(status, ignoreCase: true, out var es))
            query = query.Where(u => u.EmploymentStatus == es);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<UserListItem>(items.Select(MapItem).ToList(), total, page, pageSize);
    }

    public async Task<UserListItem> LoadAsync(long id, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new EntityNotFoundException("User", id);
        return MapItem(user);
    }

    public async Task<UserListItem> UpdateAsync(long id, UpdateUserRequest req, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new EntityNotFoundException("User", id);

        ValidateRoleCodes(req.RoleCodes);
        await ValidateManagerAsync(id, req.ManagerUserId, ct);

        var currentRoleCodes = user.UserRoles.Select(r => r.Role!.Code).ToArray();
        var removingAdmin = currentRoleCodes.Contains(Role.Admin) && !req.RoleCodes.Contains(Role.Admin);
        if (removingAdmin && !await HasAnotherActiveAdminAsync(id, ct))
            throw new CannotRemoveLastAdminException();

        user.DisplayName = req.DisplayName;
        user.DepartmentId = req.DepartmentId;
        user.ManagerUserId = req.ManagerUserId;
        user.HireDate = req.HireDate;
        user.UpdatedAt = DateTime.UtcNow;

        _db.UserRoles.RemoveRange(user.UserRoles);
        await _db.SaveChangesAsync(ct);
        await AssignRolesAsync(id, req.RoleCodes, ct);
        await _db.SaveChangesAsync(ct);

        return await LoadAsync(id, ct);
    }

    public async Task DeactivateAsync(long id, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new EntityNotFoundException("User", id);

        if (user.EmploymentStatus == EmploymentStatus.Inactive) return;

        var isAdmin = user.UserRoles.Any(r => r.Role!.Code == Role.Admin);
        if (isAdmin && !await HasAnotherActiveAdminAsync(id, ct))
            throw new CannotDeactivateLastAdminException();

        user.EmploymentStatus = EmploymentStatus.Inactive;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.RefreshTokens
            .Where(t => t.UserId == id && t.RevokedAt == null)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.RevokedAt, DateTime.UtcNow), ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ActivateAsync(long id, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw new EntityNotFoundException("User", id);
        if (user.EmploymentStatus == EmploymentStatus.Active) return;
        user.EmploymentStatus = EmploymentStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ResendInviteAsync(long id, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw new EntityNotFoundException("User", id);

        var newPassword = _passwordGen.Generate();
        user.PasswordHash = _hasher.Hash(newPassword);
        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.RefreshTokens
            .Where(t => t.UserId == id && t.RevokedAt == null)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.RevokedAt, DateTime.UtcNow), ct);
        await _db.SaveChangesAsync(ct);

        var webBase = _config["AppUrls:WebBaseUrl"] ?? "http://localhost:5173";
        var loginUrl = $"{webBase.TrimEnd('/')}/login";
        var msg = WelcomeEmailTemplate.Build(user.DisplayName, user.Email, loginUrl, newPassword, subject: "出缺勤系統登入密碼已重設");
        await _emailSender.SendAsync(msg, ct);
    }

    public async Task<IReadOnlyList<UserListItem>> SubordinatesAsync(long managerId, CancellationToken ct)
    {
        var users = await _db.Users
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.ManagerUserId == managerId && u.EmploymentStatus == EmploymentStatus.Active)
            .OrderBy(u => u.Id)
            .ToListAsync(ct);
        return users.Select(MapItem).ToList();
    }

    private static UserListItem MapItem(User u) => new(
        u.Id,
        u.Email,
        u.DisplayName,
        u.DepartmentId,
        u.Department?.Name,
        u.ManagerUserId,
        u.Manager?.DisplayName,
        u.HireDate,
        u.EmploymentStatus.ToString().ToLowerInvariant(),
        u.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Code).ToArray());

    private static void ValidateRoleCodes(IReadOnlyList<string> codes)
    {
        foreach (var code in codes)
        {
            if (!AllowedRoles.Contains(code))
                throw new InvalidRoleCodeException(code);
        }
        if (codes.Count == 0)
            throw new InvalidRoleCodeException("(empty)");
    }

    private async Task ValidateManagerAsync(long? userId, long? managerId, CancellationToken ct)
    {
        if (!managerId.HasValue) return;
        if (userId.HasValue && managerId.Value == userId.Value)
            throw new InvalidManagerSelfException();

        if (!userId.HasValue) return;

        // Walk up the chain from the proposed manager — if we hit userId within ManagerCycleDepthLimit hops, it's a cycle.
        var current = managerId.Value;
        for (int i = 0; i < ManagerCycleDepthLimit; i++)
        {
            if (current == userId.Value)
                throw new InvalidManagerCycleException();
            var next = await _db.Users.Where(u => u.Id == current).Select(u => u.ManagerUserId).FirstOrDefaultAsync(ct);
            if (!next.HasValue) return;
            current = next.Value;
        }
    }

    private async Task AssignRolesAsync(long userId, IReadOnlyList<string> roleCodes, CancellationToken ct)
    {
        var roles = await _db.Roles.Where(r => roleCodes.Contains(r.Code)).ToListAsync(ct);
        foreach (var role in roles)
            _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });
    }

    private async Task<bool> HasAnotherActiveAdminAsync(long excludeUserId, CancellationToken ct)
    {
        return await _db.UserRoles
            .Include(ur => ur.Role)
            .Include(ur => ur.User)
            .AnyAsync(ur => ur.Role!.Code == Role.Admin
                            && ur.UserId != excludeUserId
                            && ur.User!.EmploymentStatus == EmploymentStatus.Active, ct);
    }
}
