using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Orders;

public class RemovePromotionAdminSummary : Summary<RemovePromotionAdmin>
{
  public RemovePromotionAdminSummary()
  {
    Summary = "Remove a promotion from an order (staff/admin)";
    Description = "Removes a previously applied promotion from a PENDING order. Requires Staff or Admin role.";

    Response<OrderDto>(200, "Promotion removed. Returns updated order.");
    Response(400, "Order is not in PENDING status.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
    Response(404, "Order not found.");
  }
}
