using AuthApi.Contracts;
using AuthApi.Data;
using AuthApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Services;

public class CartService
{
    private readonly AppDbContext _db;
    public CartService(AppDbContext db) => _db = db;

    public async Task<Cart> EnsureCartForUserAsync(int userId)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
            cart = await _db.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId) ?? cart;
        }

        return cart;
    }

    public async Task<IEnumerable<CartItemResponseDto>> GetCartAsync(int userId)
    {
        var cart = await EnsureCartForUserAsync(userId);
        return cart.Items.Select(i => new CartItemResponseDto(
            i.ProductId,
            i.Product.Name,
            i.Product.Price,
            i.Product.ImageUrl,
            i.Quantity,
            i.Product.Stock
        )).ToList();
    }

    public async Task AddToCartAsync(int userId, int productId, int qty)
    {
        if (qty <= 0) throw new ArgumentException("Quantity must be at least 1");
        var product = await _db.Products.FindAsync(productId);
        if (product == null || !product.IsActive) throw new InvalidOperationException("Product not found");
        if (qty > product.Stock) throw new InvalidOperationException($"Only {product.Stock} items available");

        var cart = await EnsureCartForUserAsync(userId);
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existing != null)
        {
            existing.Quantity = Math.Min(existing.Quantity + qty, product.Stock);
        }
        else
        {
            cart.Items.Add(new CartItem { ProductId = productId, Quantity = qty });
        }

        await _db.SaveChangesAsync();
    }

    public async Task UpdateQuantityAsync(int userId, int productId, int qty)
    {
        var cart = await EnsureCartForUserAsync(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return;

        var product = await _db.Products.FindAsync(productId);
        if (product == null) throw new InvalidOperationException("Product not found");

        item.Quantity = Math.Min(Math.Max(1, qty), product.Stock);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(int userId, int productId)
    {
        var cart = await EnsureCartForUserAsync(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            cart.Items.Remove(item);
            await _db.SaveChangesAsync();
        }
    }

    public async Task ClearAsync(int userId)
    {
        var cart = await EnsureCartForUserAsync(userId);
        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync();
    }
}
