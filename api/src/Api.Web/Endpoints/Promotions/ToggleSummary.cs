using Api.UseCases.Promotions.DTOs;

namespace Api.Web.Endpoints.Promotions;

public class TogglePromotionSummary : Summary<TogglePromotion>
{
  public TogglePromotionSummary()
  {
    Summary = "Activate or deactivate a promotion";
    Description = "Toggles the IsActive flag on a promotion. Pass { activate: true } to enable, { activate: false } to disable. Requires Admin role.";

    Response<PromotionDto>(200, "Promotion status updated.");
    Response(401, "Authentication required.");
    Response(403, "Admin role required.");
    Response(404, "Promotion not found.");
  }
}
