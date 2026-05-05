namespace AuthApi.Contracts;

public record CartItemDto(int ProductId, int Quantity);

public record CartItemResponseDto(
    int ProductId,
    string Name,
    decimal Price,
    string? ImageUrl,
    int Quantity,
    int Stock
);

public record CartResponseDto(IEnumerable<CartItemResponseDto> Items);
