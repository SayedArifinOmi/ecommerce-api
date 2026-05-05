using AuthApi.Contracts.Checkout;
using AuthApi.Data;
using AuthApi.Models;
using AuthApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CheckoutController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CartService _cart;
    private readonly IConfiguration _config;

    public CheckoutController(AppDbContext db, CartService cart, IConfiguration config)
    {
        _db = db;
        _cart = cart;
        _config = config;
    }

    private int GetUserId() =>
        int.Parse(User.Claims.First(c => c.Type == "uid").Value);

    // 1️⃣ Create Order
    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var userId = GetUserId();
        var items = dto?.Items?.Select(i => (i.ProductId, i.Quantity)).ToList();

        if (items == null || !items.Any())
        {
            var cart = await _db.Carts
                                .Include(c => c.Items)
                                .ThenInclude(i => i.Product)
                                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return BadRequest(new { message = "Cart is empty" });

            items = cart.Items.Select(ci => (ci.ProductId, ci.Quantity)).ToList();
        }

        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
                                .Where(p => productIds.Contains(p.Id))
                                .ToDictionaryAsync(p => p.Id);

        foreach (var (pid, qty) in items)
        {
            if (!products.TryGetValue(pid, out var prod))
                return BadRequest(new { message = $"Product {pid} not found" });
            if (qty > prod.Stock)
                return BadRequest(new { message = $"Only {prod.Stock} available for {prod.Name}" });
        }

        var order = new Order
        {
            UserId = userId,
            CustomerName = User.Identity?.Name ?? "",
            CustomerEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "",
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        decimal total = 0;
        _db.Orders.Add(order);

        foreach (var (pid, qty) in items)
        {
            var p = products[pid];
            p.Stock -= qty;

            var orderItem = new OrderItem
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Quantity = qty,
                UnitPrice = p.Price,
                ImageUrl = p.ImageUrl,
                Order = order
            };

            order.Items.Add(orderItem);
            total += p.Price * qty;
        }

        order.Total = total;
        await _db.SaveChangesAsync();

        // Clear cart
        var userCart = await _db.Carts
                                .Include(c => c.Items)
                                .FirstOrDefaultAsync(c => c.UserId == userId);

        if (userCart?.Items.Any() == true)
        {
            _db.CartItems.RemoveRange(userCart.Items);
            await _db.SaveChangesAsync();
        }

        return Ok(new { orderId = order.Id, total = order.Total });
    }

    // 2️⃣ Payment
    [HttpPost("pay")]
    public async Task<IActionResult> Pay([FromBody] PayRequest req)
    {
        var order = await _db.Orders
                             .Include(o => o.Items)
                             .FirstOrDefaultAsync(o => o.Id == req.OrderId);

        if (order == null)
            return NotFound(new { message = "Order not found" });

        if (order.Status != OrderStatus.Pending)
            return BadRequest(new { message = "Order already processed" });

        var clientBase = _config["Client:BaseUrl"] ?? "http://localhost:4200";

        // Stripe Payment
        if (req.Provider.Equals("Stripe", StringComparison.OrdinalIgnoreCase))
        {
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = order.Items.Select(i => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmountDecimal = i.UnitPrice * 100,
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = i.ProductName
                        }
                    },
                    Quantity = i.Quantity
                }).ToList(),
                Mode = "payment",
                SuccessUrl = $"{clientBase}/dashboard/checkout/success?session_id={{CHECKOUT_SESSION_ID}}&orderId={order.Id}",
                CancelUrl = $"{clientBase}/dashboard/checkout/cancel",
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", order.Id.ToString() }
                }
            };

            var service = new SessionService();
            var session = service.Create(options);
            return Ok(new { sessionId = session.Id });
        }

        // COD
        else if (req.Provider.Equals("COD", StringComparison.OrdinalIgnoreCase))
        {
            order.Status = OrderStatus.Placed;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Order placed successfully (COD)" });
        }

        return BadRequest(new { message = "Unknown payment provider" });
    }

    // 3️⃣ Confirm Stripe Payment
    [HttpPost("confirm-stripe/{sessionId}")]
    public async Task<IActionResult> ConfirmStripePayment(string sessionId)
    {
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        var service = new SessionService();
        var session = service.Get(sessionId);

        if (session.PaymentStatus == "paid")
        {
            if (session.Metadata.TryGetValue("orderId", out var orderIdStr) &&
                int.TryParse(orderIdStr, out var orderId))
            {
                var order = await _db.Orders.FindAsync(orderId);
                if (order != null)
                {
                    order.Status = OrderStatus.Paid;
                    await _db.SaveChangesAsync();
                }
            }
        }

        return Ok(new { message = "Payment confirmed" });
    }

    // 4️⃣ Get My Orders
    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetUserId();
        var orders = await _db.Orders
                              .Where(o => o.UserId == userId)
                              .Include(o => o.Items)
                              .OrderByDescending(o => o.CreatedAt)
                              .AsNoTracking()
                              .ToListAsync();

        return Ok(orders);
    }
}
