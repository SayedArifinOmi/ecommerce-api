using System.ComponentModel.DataAnnotations;

namespace AuthApi.Models;
public class Product
{
    public int Id { get; set; }
    [MaxLength(128)] public string Name { get; set; } = "";
    [MaxLength(64)] public string Brand { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
