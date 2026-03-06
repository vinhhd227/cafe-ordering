using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Orders;

public class CreateSummary : Summary<Create>
{
  public CreateSummary()
  {
    Summary = "Place a new order in an active session";
    Description =
      "Creates a new order associated with the provided session. " +
      "The session must exist and be active. At least one item is required. " +
      "No authentication required — any guest with a valid session ID can place an order.";

    ExampleRequest = new CreateOrderRequest
    {
      SessionId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
      Items =
      [
        new CreateOrderItemRequest
        {
          ProductId = 1, ProductName = "Cà phê sữa đá", UnitPrice = 35000, Quantity = 2
        }
      ]
    };

    Response<PlaceOrderResponseDto>(200, "Order created successfully.",
      example: new PlaceOrderResponseDto(42, "ORD-20260101120000", 70000));
    Response(400, "Validation error: session not found, closed session, or no items provided.");
    Response(409, "Session is closed.");
  }
}
