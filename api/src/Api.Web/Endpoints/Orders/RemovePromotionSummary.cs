using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Orders;

public class RemovePromotionSummary : Summary<RemovePromotion>
{
  public RemovePromotionSummary()
  {
    Summary = "Remove a promotion from an order (client)";
    Description = "Removes a previously applied promotion from a PENDING order. Requires SessionId for ownership check.";

    Response<OrderDto>(200, "Promotion removed. Returns updated order.");
    Response(400, "Order is not in PENDING status.");
    Response(403, "Session does not own this order.");
    Response(404, "Order not found.");
  }
}
