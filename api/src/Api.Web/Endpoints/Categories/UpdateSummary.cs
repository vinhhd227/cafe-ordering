namespace Api.Web.Endpoints.Categories;

public class UpdateSummary : Summary<Update>
{
  public UpdateSummary()
  {
    Summary = "Rename a category";
    Description =
      "Updates the display name of an existing category. " +
      "Only the name can be changed through this endpoint; " +
      "use the activate/deactivate endpoints to toggle the category's visibility.";

    Params["CategoryId"] = "The integer ID of the category to rename.";

    ExampleRequest = new UpdateCategoryRequest
    {
      CategoryId = 1,
      Name = "Hot Beverages"
    };

    Response(200, "Category renamed successfully.");
    Response(400, "Validation failed — e.g. the name is empty or already taken.");
    Response(404, "No category with the given ID was found.");
  }
}
