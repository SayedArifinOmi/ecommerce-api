namespace AuthApi.Contracts.Checkout;

public record CreateOrderItemDto(int ProductId, int Quantity);
public record CreateOrderDto(IEnumerable<CreateOrderItemDto> Items);

public record PayRequest(int OrderId, string Provider);
public record PayResponse(string? RedirectUrl, string? SessionId);
