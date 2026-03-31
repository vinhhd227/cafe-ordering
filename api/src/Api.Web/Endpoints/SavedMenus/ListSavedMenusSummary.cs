using Api.UseCases.SavedMenus.DTOs;

namespace Api.Web.Endpoints.SavedMenus;

public class ListSavedMenusSummary : Summary<ListSavedMenus>
{
  public ListSavedMenusSummary()
  {
    Summary = "List saved menu designs";
    Description = "Returns all saved menu designs for the current tenant.";
    Response<List<SavedMenuDto>>(200, "List retrieved successfully");
    Response(401, "Unauthorized");
    Response(403, "Forbidden");
  }
}
