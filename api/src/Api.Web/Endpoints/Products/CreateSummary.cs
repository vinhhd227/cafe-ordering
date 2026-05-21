namespace Api.Web.Endpoints.Products;

public class CreateSummary : Summary<Create>
{
  public CreateSummary()
  {
    Summary = "Táº¡o sáº£n pháº©m má»›i";
    Description =
      "ThÃªm sáº£n pháº©m má»›i vÃ o danh má»¥c. CÃ³ thá»ƒ truyá»n kÃ¨m variantGroups Ä‘á»ƒ táº¡o variant " +
      "(size, nhiá»‡t Ä‘á»™, ...) trong cÃ¹ng má»™t request â€” backend dÃ¹ng transaction Ä‘áº£m báº£o rollback " +
      "toÃ n bá»™ náº¿u cÃ³ lá»—i. Náº¿u khÃ´ng truyá»n variantGroups (hoáº·c Ä‘á»ƒ null), dÃ¹ng " +
      "PUT /api/products/{id}/variant-groups Ä‘á»ƒ cáº¥u hÃ¬nh sau.";

    ExampleRequest = new CreateProductRequest
    {
      CategoryId = 1,
      Name = "Caramel Macchiato",
      Price = 65000,
      Description = "Espresso layered with vanilla-flavoured syrup, steamed milk, and a caramel drizzle.",
      ImageUrl = "https://example.com/images/caramel-macchiato.jpg",
      VariantGroups =
      [
        new CreateVariantGroupRequest
        {
          Name = "Nhiá»‡t Ä‘á»™", IsRequired = true, SelectionType = "Single",
          Values =
          [
            new CreateVariantValueRequest { Label = "NÃ³ng", Price = 0, IsDefault = true },
            new CreateVariantValueRequest { Label = "Láº¡nh", Price = 0, IsDefault = false },
          ],
        },
        new CreateVariantGroupRequest
        {
          Name = "Size", IsRequired = false, SelectionType = "Single",
          Values =
          [
            new CreateVariantValueRequest { Label = "M", Price = 0,    IsDefault = true },
            new CreateVariantValueRequest { Label = "L", Price = 5000, IsDefault = false },
          ],
        },
      ],
    };

    Response(201, "Sáº£n pháº©m Ä‘Æ°á»£c táº¡o thÃ nh cÃ´ng. Tráº£ vá» Id cá»§a sáº£n pháº©m má»›i.");
    Response(400, "Dá»¯ liá»‡u khÃ´ng há»£p lá»‡.");
    Response(404, "CategoryId khÃ´ng tá»“n táº¡i.");
  }
}
