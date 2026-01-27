namespace api.Contracts.Orders;

public static class CreateOrder
{
    public record Request(
        string CustomerName, 
        string TableNumber, 
        List<OrderItemDto> Items
    );

    public record Response(
        Guid OrderId, 
        string Status, 
        decimal TotalAmount
    );

    // Bạn có thể để các DTO phụ liên quan trực tiếp ở đây
    public record OrderItemDto(Guid ProductId, int Quantity);
}