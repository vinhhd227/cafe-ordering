using Api.UseCases.SavedMenus.DTOs;

namespace Api.Web.Endpoints.SavedMenus;

public class CreateSavedMenuSummary : Summary<CreateSavedMenu>
{
  public CreateSavedMenuSummary()
  {
    Summary = "Save a menu design";
    Description = "Persists the current menu design (product selection, layout, colors, template) with a given name.";
    Response<SavedMenuDto>(200, "Saved successfully");
    Response(400, "Invalid input");
    Response(401, "Unauthorized");
    Response(403, "Forbidden");
  }
}
