using Api.UseCases.Promotions.Validate;

namespace Api.Web.Endpoints.Promotions;

public class ValidatePromotionSummary : Summary<ValidatePromotion>
{
  public ValidatePromotionSummary()
  {
    Summary = "Validate a promotion code and preview discount";
    Description =
      "Validates a promotion code and returns discount preview. " +
      "Optionally pass orderAmount to estimate the discount. Public endpoint — no auth required.";

    Response<ValidatePromotionResult>(200, "Validation result (includes isApplicable flag and estimated discount).");
    Response(404, "Promotion code not found.");
  }
}
