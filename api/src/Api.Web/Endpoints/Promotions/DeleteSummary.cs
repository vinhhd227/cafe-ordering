namespace Api.Web.Endpoints.Promotions;

public class DeletePromotionSummary : Summary<DeletePromotion>
{
  public DeletePromotionSummary()
  {
    Summary = "Soft-delete a promotion";
    Description = "Soft-deletes a promotion. It will no longer be visible or applicable. Requires Admin role.";

    Response(200, "Promotion deleted successfully.");
    Response(401, "Authentication required.");
    Response(403, "Admin role required.");
    Response(404, "Promotion not found.");
  }
}
