using AttendanceSystem.Api.Contracts;
using AttendanceSystem.Api.Services;
using AttendanceSystem.Domain.Entities;
using AttendanceSystem.Domain.Exceptions;
using AttendanceSystem.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceSystem.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _users;
    private readonly ICurrentUser _currentUser;

    public UsersController(UserService users, ICurrentUser currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = Role.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        var created = await _users.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpGet]
    [Authorize(Roles = Role.Admin)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] long? departmentId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _users.ListAsync(page, pageSize, search, departmentId, status, ct);
        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var id = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        return Ok(await _users.LoadAsync(id, ct));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        if (!_currentUser.IsInRole(Role.Admin) && _currentUser.UserId != id)
            throw new ForbiddenException();
        return Ok(await _users.LoadAsync(id, ct));
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var updated = await _users.UpdateAsync(id, req, ct);
        return Ok(updated);
    }

    [HttpPost("{id:long}/deactivate")]
    [Authorize(Roles = Role.Admin)]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        await _users.DeactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/activate")]
    [Authorize(Roles = Role.Admin)]
    public async Task<IActionResult> Activate(long id, CancellationToken ct)
    {
        await _users.ActivateAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _users.DeactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/resend-invite")]
    [Authorize(Roles = Role.Admin)]
    public async Task<IActionResult> ResendInvite(long id, CancellationToken ct)
    {
        await _users.ResendInviteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:long}/subordinates")]
    public async Task<IActionResult> Subordinates(long id, CancellationToken ct)
    {
        if (!_currentUser.IsInRole(Role.Admin) && _currentUser.UserId != id)
            throw new ForbiddenException();
        return Ok(await _users.SubordinatesAsync(id, ct));
    }
}
