using Api.UseCases.Promotions.DTOs;

namespace Api.Web.Endpoints.Promotions;

public class GetPromotionByIdSummary : Summary<GetPromotionById>
{
  public GetPromotionByIdSummary()
  {
    Summary = "Get promotion by ID";
    Description = "Returns a single promotion by its ID. Requires Staff or Admin role.";

    Response<PromotionDto>(200, "Promotion found.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
    Response(404, "Promotion not found.");
  }
}
