using Api.UseCases.Menu.DTOs;

namespace Api.Web.Endpoints.Menu;

public class GetSummary : Summary<Get>
{
  public GetSummary()
  {
    Summary = "Get active menu";
    Description =
      "Returns all active categories along with their active products. " +
      "Categories with no active products are excluded. " +
      "This endpoint is public and does not require authentication.";

    Response<List<MenuCategoryDto>>(200, "Returns the list of active categories with their nested active products.");
  }
}
