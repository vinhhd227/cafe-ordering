namespace Api.Web.Endpoints.Categories;

public class ReorderSummary : Summary<Reorder>
{
  public ReorderSummary()
  {
    Summary = "Reorder categories";
    Description =
      "Updates the display order of categories by accepting an ordered list of category IDs. " +
      "The first ID in the array becomes SortOrder 1, the second becomes SortOrder 2, and so on. " +
      "IDs not present in the list are not affected.";

    ExampleRequest = new ReorderCategoriesRequest
    {
      Ids = [3, 1, 5, 2, 4]
    };

    Response(204, "Sort order updated successfully.");
    Response(400, "Validation failed — e.g. the IDs list is empty or malformed.");
    Response(401, "Not authenticated.");
    Response(403, "Missing category.update permission.");
  }
}
