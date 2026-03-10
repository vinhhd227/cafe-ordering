using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Orders;

public class ApplyPromotionSummary : Summary<ApplyPromotion>
{
  public ApplyPromotionSummary()
  {
    Summary = "Apply a promotion to an order (client)";
    Description =
      "Applies a promotion code to a PENDING order. SessionId must match the order's session for ownership verification.";

    Response<OrderDto>(200, "Promotion applied. Returns updated order with TotalDiscount and FinalAmount.");
    Response(400, "Promotion invalid, expired, usage limit reached, or stack policy conflict.");
    Response(403, "Session does not own this order.");
    Response(404, "Order or promotion not found.");
  }
}
