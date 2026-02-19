namespace Api.Web.Endpoints.Products;

public class DeleteSummary : Summary<Delete>
{
  public DeleteSummary()
  {
    Summary = "Soft-delete a product";
    Description =
      "Marks the product as deleted without removing it from the database (soft delete). " +
      "Soft-deleted products are excluded from all public listing and detail endpoints " +
      "but remain in the database to preserve historical order references. " +
      "The identity of the requester is recorded in the `deletedBy` audit field. " +
      "This operation cannot be undone through the public API.";

    Params["ProductId"] = "The integer ID of the product to soft-delete.";

    Response(200, "Product successfully marked as deleted.");
    Response(404, "No active product with the given ID was found.");
  }
}
