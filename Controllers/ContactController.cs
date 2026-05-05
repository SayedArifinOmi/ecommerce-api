using AuthApi.Contracts.Contact;
using AuthApi.Data;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ContactController(AppDbContext db) => _db = db;

        // Public: submit message
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateMessage([FromBody] ContactDto dto)
        {
            if (dto == null) return BadRequest(new { message = "Invalid payload" });

            try
            {
                var contact = new Contact
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    Subject = dto.Subject,
                    Message = dto.Message,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _db.Contact.Add(contact);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Message saved successfully" });
            }
            catch (Exception ex)
            {
                // return full exception string in dev; cleaner in prod
                return StatusCode(500, new { message = "An error occurred", detail = ex.ToString() });
            }
        }

        // Admin: list all messages (requires Admin role)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.Contact
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return Ok(list);
        }

        // Admin: get single message
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Get(int id)
        {
            var msg = await _db.Contact.FindAsync(id);
            if (msg == null) return NotFound();
            return Ok(msg);
        }

        // Admin: mark read/unread
        [HttpPatch("{id:int}/read")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var msg = await _db.Contact.FindAsync(id);
            if (msg == null) return NotFound();
            msg.IsRead = true;
            await _db.SaveChangesAsync();
            return Ok(msg);
        }

        // Admin: delete
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var msg = await _db.Contact.FindAsync(id);
            if (msg == null) return NotFound();
            _db.Contact.Remove(msg);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
