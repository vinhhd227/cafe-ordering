using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Orders;

public class ApplyPromotionAdminSummary : Summary<ApplyPromotionAdmin>
{
  public ApplyPromotionAdminSummary()
  {
    Summary = "Apply a promotion to an order (staff/admin)";
    Description = "Applies a promotion by code or ID to a PENDING order. Requires Staff or Admin role.";

    Response<OrderDto>(200, "Promotion applied. Returns updated order.");
    Response(400, "Promotion invalid, expired, or stack policy conflict.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
    Response(404, "Order or promotion not found.");
  }
}
