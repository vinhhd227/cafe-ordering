using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.UpdateItem;

/// <summary>
/// Set quantity of a product in an existing Pending order.
/// quantity = 0 removes the item; quantity &gt; 0 adds or updates it.
/// When SessionId is provided the handler enforces session ownership (customer use).
/// </summary>
public record UpdateOrderItemCommand(int OrderId, int ProductId, int Quantity, Guid? SessionId = null)
  : ICommand<Result<OrderDto>>;
