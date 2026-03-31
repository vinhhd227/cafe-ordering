namespace Api.Web.Endpoints.SavedMenus;

public class DeleteSavedMenuSummary : Summary<DeleteSavedMenu>
{
  public DeleteSavedMenuSummary()
  {
    Summary = "Delete a saved menu design";
    Description = "Soft-deletes a saved menu design by ID.";
    Response(204, "Deleted successfully");
    Response(404, "Menu not found");
    Response(401, "Unauthorized");
    Response(403, "Forbidden");
  }
}
