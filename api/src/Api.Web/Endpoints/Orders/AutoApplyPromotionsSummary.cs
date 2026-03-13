using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Orders;

public class AutoApplyPromotionsSummary : Summary<AutoApplyPromotions>
{
  public AutoApplyPromotionsSummary()
  {
    Summary = "Auto-apply all eligible promotions without a code";
    Description =
      "Finds all active promotions that have no code and automatically applies any that are eligible " +
      "for the given order (valid date, usage not exceeded, min order amount met, items match scope). " +
      "Promotions that conflict with stack policy or produce zero discount are silently skipped. " +
      "Requires StaffOrAdmin role.";

    ExampleRequest = new AutoApplyPromotionsRequest { OrderId = 1 };

    Response<OrderDto>(200, "Order updated with all auto-applied promotions.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions — StaffOrAdmin role required.");
    Response(404, "Order not found.");
  }
}
