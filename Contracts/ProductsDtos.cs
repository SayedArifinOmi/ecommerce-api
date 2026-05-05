using System.ComponentModel.DataAnnotations;

namespace AuthApi.Contracts;

public class ProductCreateDto
{
    [Required, MaxLength(128)] public string Name { get; set; } = "";
    [Required, MaxLength(64)] public string Brand { get; set; } = "";
    public string? Description { get; set; }
    [Range(0, double.MaxValue)] public decimal Price { get; set; }
    [Range(0, int.MaxValue)] public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public record PagedResult<T>(IEnumerable<T> Items, int Total);
