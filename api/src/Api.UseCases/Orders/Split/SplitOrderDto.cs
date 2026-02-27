namespace Api.UseCases.Orders.Split;

public record SplitItemRequest(int ProductId, int Quantity);

public record SplitOrderResult(int NewOrderId, string NewOrderNumber);
