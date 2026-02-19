namespace Api.Web.Endpoints.Products;

public class UpdateSummary : Summary<Update>
{
  public UpdateSummary()
  {
    Summary = "Update an existing product";
    Description =
      "Updates the display name, price, description, and image URL of a product. " +
      "This is a full replacement — all four fields must be provided even if only one changes. " +
      "Customization flags (temperature, ice level, sugar level) are not changed by this endpoint; " +
      "use a dedicated admin endpoint for those.";

    Params["ProductId"] = "The integer ID of the product to update.";

    ExampleRequest = new UpdateProductRequest
    {
      ProductId = 1,
      Name = "Caramel Macchiato (Large)",
      Price = 75000,
      Description = "Large serving of espresso with vanilla syrup, steamed milk, and caramel drizzle.",
      ImageUrl = "https://example.com/images/caramel-macchiato-large.jpg"
    };

    Response(200, "Product updated successfully.");
    Response(400, "Validation failed — e.g. name is empty or price is non-positive.");
    Response(404, "No active product with the given ID was found.");
  }
}
