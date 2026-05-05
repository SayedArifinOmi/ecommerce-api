using AuthApi.Contracts;
using AuthApi.Data;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<ActionResult<PagedResult<Product>>> List(
        string? search = "", int page = 1, int pageSize = 10, string sort = "CreatedAt", string dir = "desc")
    {
        var q = _db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) || p.Brand.Contains(search));

        q = (sort.ToLower(), dir.ToLower()) switch
        {
            ("name", "asc") => q.OrderBy(p => p.Name),
            ("name", "desc") => q.OrderByDescending(p => p.Name),
            ("brand", "asc") => q.OrderBy(p => p.Brand),
            ("brand", "desc") => q.OrderByDescending(p => p.Brand),
            ("price", "asc") => q.OrderBy(p => p.Price),
            ("price", "desc") => q.OrderByDescending(p => p.Price),
            _ when dir == "asc" => q.OrderBy(p => p.CreatedAt),
            _ => q.OrderByDescending(p => p.CreatedAt)
        };

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResult<Product>(items, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> Get(int id)
    {
        var found = await _db.Products.FindAsync(id);
        return found is null ? NotFound() : Ok(found);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Product>> Create(ProductCreateDto dto)
    {
        var p = new Product
        {
            Name = dto.Name,
            Brand = dto.Brand,
            Description = dto.Description,
            Price = dto.Price,
            Stock = dto.Stock,
            ImageUrl = dto.ImageUrl,
            IsActive = dto.IsActive
        };
        _db.Products.Add(p);
        await _db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Product>> Update(int id, ProductCreateDto dto)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();
        p.Name = dto.Name; p.Brand = dto.Brand; p.Description = dto.Description;
        p.Price = dto.Price; p.Stock = dto.Stock; p.ImageUrl = dto.ImageUrl; p.IsActive = dto.IsActive;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();
        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
