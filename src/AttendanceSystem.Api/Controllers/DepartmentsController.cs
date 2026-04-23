using AttendanceSystem.Api.Contracts;
using AttendanceSystem.Api.Services;
using AttendanceSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceSystem.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly DepartmentService _departments;

    public DepartmentsController(DepartmentService departments)
    {
        _departments = departments;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await _departments.ListAsync(ct));

    [HttpPost]
    [Authorize(Roles = Role.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest req, CancellationToken ct)
    {
        var created = await _departments.CreateAsync(req, ct);
        return CreatedAtAction(nameof(List), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateDepartmentRequest req, CancellationToken ct)
    {
        var updated = await _departments.UpdateAsync(id, req, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _departments.DeleteAsync(id, ct);
        return NoContent();
    }
}
