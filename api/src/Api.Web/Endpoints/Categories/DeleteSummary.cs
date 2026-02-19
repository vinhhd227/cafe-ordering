namespace Api.Web.Endpoints.Categories;

public class DeleteSummary : Summary<Delete>
{
  public DeleteSummary()
  {
    Summary = "Soft-delete a category";
    Description =
      "Marks the category as deleted without removing it from the database (soft delete). " +
      "All products belonging to this category are also hidden from public listings " +
      "as a side-effect. The record and its audit trail are preserved for historical reporting. " +
      "The identity of the requester is stored in the `deletedBy` audit field. " +
      "This operation cannot be undone through the public API.";

    Params["CategoryId"] = "The integer ID of the category to soft-delete.";

    Response(200, "Category successfully marked as deleted.");
    Response(404, "No category with the given ID was found.");
  }
}
