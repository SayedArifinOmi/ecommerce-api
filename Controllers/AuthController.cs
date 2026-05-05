using AuthApi.Contracts;
using AuthApi.Data;
using AuthApi.Models;
using AuthApi.Services;
using AuthApi.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using BCryptNet = BCrypt.Net.BCrypt;
using AuthApi.Options;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IEmailSender _email;
    private readonly ClientOptions _client;

    public AuthController(AppDbContext db, IEmailSender email, IOptions<ClientOptions> client , ITokenService tokens)
    {
        _db = db;
        _email = email;
        _client = client.Value;
        _tokens = tokens;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email || u.Username == dto.Username))
            return BadRequest(new { message = "User already exists" });

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            Role = string.IsNullOrWhiteSpace(dto.Role) ? "User" : dto.Role
        };
        user.PasswordHash = BCryptNet.HashPassword(dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Registered" });
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return Unauthorized(new { message = "Invalid credentials" });

        if (!BCryptNet.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        var access = _tokens.CreateAccessToken(user);
        var (refresh, expiry) = _tokens.CreateRefreshToken();

        user.RefreshToken = refresh;
        user.RefreshTokenExpiry = expiry;
        await _db.SaveChangesAsync();

        return Ok(new TokenResponse(access, refresh));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.RefreshToken == req.RefreshToken);
        if (user == null || user.RefreshTokenExpiry == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Unauthorized(new { message = "Invalid refresh token" });

        var access = _tokens.CreateAccessToken(user);
        var (refresh, expiry) = _tokens.CreateRefreshToken();
        user.RefreshToken = refresh;
        user.RefreshTokenExpiry = expiry;
        await _db.SaveChangesAsync();

        return Ok(new TokenResponse(access, refresh));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var name = User.Identity?.Name;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == name);
        if (user == null) return Ok();

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Logged out" });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly() => Ok(new { message = "Admin data" });

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        // Always return same message to avoid enumeration
        if (user == null)
            return Ok(new { message = "If that email exists, a reset code has been sent." });

        // generate secure 6-digit code
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        // hash & save
        user.ResetCodeHash = Crypto.Sha256(code);
        user.ResetCodeExpiry = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();

        // email body
        var html = $@"
            <p>Assalamualaikum {user.Username},</p>
            <p>Your password reset code is: <strong>{code}</strong></p>
            <p>This code will expire in 15 minutes.</p>
        ";

        // send email
        await _email.SendAsync(user.Email, "Your password reset code", html);

        return Ok(new { message = "If that email exists, a reset code has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return BadRequest(new { message = "Invalid code or email" });

        if (user.ResetCodeHash == null || user.ResetCodeExpiry == null || user.ResetCodeExpiry < DateTime.UtcNow)
            return BadRequest(new { message = "Invalid or expired code" });

        var incomingHash = Crypto.Sha256(dto.Code ?? "");
        if (!string.Equals(incomingHash, user.ResetCodeHash, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Invalid or expired code" });

        // update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.ResetCodeHash = null;
        user.ResetCodeExpiry = null;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password reset successfully" });
    }
   // [HttpGet("test-email")]
    //public async Task<IActionResult> TestEmail()
    //{
      //  await _email.SendAsync("recipient@gmail.com", "Test Email", "<p>Hello! Email works!</p>");
        //return Ok("Email sent successfully");
    //}

}





