using Api.UseCases.Promotions.DTOs;

namespace Api.Web.Endpoints.Promotions;

public class CreatePromotionSummary : Summary<CreatePromotion>
{
  public CreatePromotionSummary()
  {
    Summary = "Create a new promotion";
    Description =
      "Creates a promotion with a specified discount type (PERCENTAGE, FIXED, BUY_X_GET_Y), " +
      "scope (ORDER, PRODUCT, CATEGORY), and stack policy. Requires Admin role.";

    Response<PromotionDto>(200, "Promotion created successfully.");
    Response(400, "Validation failed (invalid discount type, scope, missing required fields, etc.).");
    Response(401, "Authentication required.");
    Response(403, "Admin role required.");
  }
}
