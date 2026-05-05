using AuthApi.Contracts;
using AuthApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCryptNet = BCrypt.Net.BCrypt;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProfileController(AppDbContext db) { _db = db; }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var name = User.Identity?.Name;
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Username == name);
        if (u is null) return NotFound();
        return Ok(new { u.Id, u.Username, u.Email, u.Role });
    }

    [HttpPut("me")]
    public async Task<IActionResult> Update(ProfileUpdateDto dto)
    {
        var name = User.Identity?.Name;
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Username == name);
        if (u is null) return NotFound();
        u.Username = dto.Username;
        await _db.SaveChangesAsync();
        return Ok(new { u.Id, u.Username, u.Email, u.Role });
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var name = User.Identity?.Name;
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Username == name);
        if (u is null) return NotFound();

        if (!BCryptNet.Verify(dto.CurrentPassword, u.PasswordHash))
            return BadRequest(new { message = "Current password incorrect" });

        u.PasswordHash = BCryptNet.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
