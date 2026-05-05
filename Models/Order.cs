namespace AuthApi.Models;
using System.Text.Json.Serialization;

public enum OrderStatus { Pending, Paid, Shipped, Delivered, Cancelled,
    Placed
}

public class Order
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }

    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
   
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string PaymentMethod { get; set; } = "Bkash";
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    [JsonIgnore]
    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public string ProductName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
