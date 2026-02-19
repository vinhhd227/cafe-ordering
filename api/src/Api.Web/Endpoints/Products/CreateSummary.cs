namespace Api.Web.Endpoints.Products;

public class CreateSummary : Summary<Create>
{
  public CreateSummary()
  {
    Summary = "Create a new product";
    Description =
      "Adds a new product to the specified category. " +
      "Customization flags (temperature, ice level, sugar level) control which order options " +
      "are displayed to customers when they add the item to their cart. " +
      "The product is active by default and appears on the public menu immediately after creation.";

    ExampleRequest = new CreateProductRequest
    {
      CategoryId = 1,
      Name = "Caramel Macchiato",
      Price = 65000,
      Description = "Espresso layered with vanilla-flavoured syrup, steamed milk, and a caramel drizzle.",
      ImageUrl = "https://example.com/images/caramel-macchiato.jpg",
      HasTemperatureOption = true,
      HasIceLevelOption = true,
      HasSugarLevelOption = true
    };

    Response(201, "Product created successfully. Returns the new product's integer ID.");
    Response(400, "Validation failed — e.g. name is empty, price is non-positive, or required fields are missing.");
    Response(404, "The specified CategoryId does not exist.");
  }
}
