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
      CostPrice: null,
      DiscountPrice: null,
      Sku: null,
      Barcode: null,
      IsActive: true,
      ImageUrl: "https://example.com/images/caramel-macchiato.jpg",
      IsAccompaniment: false,
      EstimatedPrepMinutes: 5,
      VariantGroups:
      [
        new ProductVariantGroupDto(1, "Nhiá»‡t Ä‘á»™", true, "Single", 1,
        [
          new ProductVariantValueDto(1, "NÃ³ng", 0, true, 1),
          new ProductVariantValueDto(2, "Láº¡nh", 0, false, 2),
        ]),
        new ProductVariantGroupDto(2, "Size", false, "Single", 2,
        [
          new ProductVariantValueDto(3, "M", 0, true, 1),
          new ProductVariantValueDto(4, "L", 5000, false, 2),
        ]),
      ],
      Variants:
      [
        new ProductVariantDto(1, 65000, null, null, null, true, 1, [1, 3]),
        new ProductVariantDto(2, 70000, null, null, null, true, 2, [1, 4]),
      ],
      AssignedOptionGroupIds: [1, 2],
      CreatedAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
      UpdatedAt: null);

    Response<ProductDto>(200, "Returns the full product detail.");
    Response(404, "No active product with the given ID was found.");
  }
}
