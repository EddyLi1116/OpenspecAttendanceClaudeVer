using AttendanceSystem.Api.Contracts;
using AttendanceSystem.Domain.Entities;
using AttendanceSystem.Domain.Exceptions;
using AttendanceSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Api.Services;

public class DepartmentService
{
    private readonly AttendanceDbContext _db;

    public DepartmentService(AttendanceDbContext db) => _db = db;

    public async Task<IReadOnlyList<DepartmentDto>> ListAsync(CancellationToken ct)
    {
        var items = await _db.Departments.OrderBy(d => d.Name).ToListAsync(ct);
        return items.Select(d => new DepartmentDto(d.Id, d.Code, d.Name)).ToList();
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest req, CancellationToken ct)
    {
        if (await _db.Departments.AnyAsync(d => d.Code == req.Code, ct))
            throw new DepartmentCodeExistsException(req.Code);

        var now = DateTime.UtcNow;
        var dept = new Department { Code = req.Code, Name = req.Name, CreatedAt = now, UpdatedAt = now };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync(ct);
        return new DepartmentDto(dept.Id, dept.Code, dept.Name);
    }

    public async Task<DepartmentDto> UpdateAsync(long id, UpdateDepartmentRequest req, CancellationToken ct)
    {
        var dept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct)
                   ?? throw new EntityNotFoundException("Department", id);

        if (dept.Code != req.Code && await _db.Departments.AnyAsync(d => d.Code == req.Code, ct))
            throw new DepartmentCodeExistsException(req.Code);

        dept.Code = req.Code;
        dept.Name = req.Name;
        dept.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new DepartmentDto(dept.Id, dept.Code, dept.Name);
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var dept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct)
                   ?? throw new EntityNotFoundException("Department", id);
        if (await _db.Users.AnyAsync(u => u.DepartmentId == id, ct))
            throw new DepartmentHasMembersException();
        _db.Departments.Remove(dept);
        await _db.SaveChangesAsync(ct);
    }
}
