namespace AuthApi.Contracts.Orders;

public record SetOrderStatusDto(string Status);
public record OrderCreatedResponse(int OrderId, decimal Total);
