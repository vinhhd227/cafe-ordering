using Api.UseCases.Promotions.List;

namespace Api.Web.Endpoints.Promotions;

public class ListPromotionsSummary : Summary<ListPromotions>
{
  public ListPromotionsSummary()
  {
    Summary = "List promotions with filtering and pagination";
    Description =
      "Returns a paged list of promotions. Filter by isActive, scope, and discountType. Requires Staff or Admin role.";

    Response<PagedPromotionsDto>(200, "Paged list of promotions.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}
