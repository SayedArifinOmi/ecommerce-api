using AuthApi.Contracts.Orders;
using AuthApi.Data;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) => _db = db;

    private int GetUserId() => int.Parse(User.Claims.First(c => c.Type == "uid").Value);

    // User: Get my orders
    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetUserId();
        var orders = await _db.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)           // Include items in single query
            .AsNoTracking()                  // No tracking for read-only
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders);
    }

    // Admin: List all orders
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> List(string? search = "", int page = 1, int pageSize = 10, OrderStatus? status = null)
    {
        var q = _db.Orders
                   .Include(o => o.Items)     // Include items
                   .AsNoTracking()
                   .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(o => o.CustomerName.Contains(search) || o.CustomerEmail.Contains(search));
        if (status.HasValue)
            q = q.Where(o => o.Status == status);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { items, total });
    }

    // Admin: Update order status
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetStatus(int id, SetOrderStatusDto dto)
    {
        if (!Enum.TryParse<OrderStatus>(dto.Status, out var status))
            return BadRequest("Invalid status");

        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
