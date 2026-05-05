using AuthApi.Contracts;
using AuthApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var users = await _db.Users
            .Select(u => new { u.Id, u.Username, u.Email, u.Role })
            .ToListAsync();
        return Ok(users);
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> SetRole(int id, SetRoleDto dto)
    {
        var u = await _db.Users.FindAsync(id);
        if (u is null) return NotFound();
        u.Role = dto.Role;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
