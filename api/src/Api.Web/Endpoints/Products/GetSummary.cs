using Api.UseCases.Products.DTOs;

namespace Api.Web.Endpoints.Products;

public class GetSummary : Summary<Get>
{
  public GetSummary()
  {
    Summary = "Get a product by ID";
    Description =
      "Returns the full detail record for a single active product, " +
      "including category name, price, description, image URL, and all customization flags.";

    Params["ProductId"] = "The integer ID of the product to retrieve.";

    ResponseExamples[200] = new ProductDto(
      Id: 1,
      CategoryId: 1,
      CategoryName: "Coffee",
      Name: "Caramel Macchiato",
      Description: "Espresso layered with vanilla-flavoured syrup, steamed milk, and a caramel drizzle.",
      Price: 65000,
      IsActive: true,
      ImageUrl: "https://example.com/images/caramel-macchiato.jpg",
      HasTemperatureOption: true,
      HasIceLevelOption: true,
      HasSugarLevelOption: true,
      CreatedAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
      UpdatedAt: null);

    Response<ProductDto>(200, "Returns the full product detail.");
    Response(404, "No active product with the given ID was found.");
  }
}
