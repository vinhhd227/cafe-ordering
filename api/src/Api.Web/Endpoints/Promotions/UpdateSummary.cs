using Api.UseCases.Promotions.DTOs;

namespace Api.Web.Endpoints.Promotions;

public class UpdatePromotionSummary : Summary<UpdatePromotion>
{
  public UpdatePromotionSummary()
  {
    Summary = "Update a promotion";
    Description = "Updates all fields of an existing promotion. Requires Admin role.";

    Response<PromotionDto>(200, "Promotion updated successfully.");
    Response(400, "Validation failed or duplicate code.");
    Response(401, "Authentication required.");
    Response(403, "Admin role required.");
    Response(404, "Promotion not found.");
  }
}
