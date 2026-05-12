namespace Api.Web.Endpoints.Products;

public class CreateSummary : Summary<Create>
{
  public CreateSummary()
  {
    Summary = "Tạo sản phẩm mới";
    Description =
      "Thêm sản phẩm mới vào danh mục. Có thể truyền kèm attributeGroups để tạo attribute " +
      "(size, nhiệt độ, ...) trong cùng một request — backend dùng transaction đảm bảo rollback " +
      "toàn bộ nếu có lỗi. Nếu không truyền attributeGroups (hoặc để null), dùng " +
      "PUT /api/products/{id}/option-groups để cấu hình sau.";

    ExampleRequest = new CreateProductRequest
    {
      CategoryId = 1,
      Name = "Caramel Macchiato",
      Price = 65000,
      Description = "Espresso layered with vanilla-flavoured syrup, steamed milk, and a caramel drizzle.",
      ImageUrl = "https://example.com/images/caramel-macchiato.jpg",
      AttributeGroups =
      [
        new CreateAttributeGroupRequest
        {
          Name = "Nhiệt độ", IsRequired = true, SelectionType = "Single",
          Values =
          [
            new CreateAttributeValueRequest { Label = "Nóng", PriceAdjustment = 0, IsDefault = true },
            new CreateAttributeValueRequest { Label = "Lạnh", PriceAdjustment = 0, IsDefault = false },
          ],
        },
        new CreateAttributeGroupRequest
        {
          Name = "Size", IsRequired = false, SelectionType = "Single",
          Values =
          [
            new CreateAttributeValueRequest { Label = "M", PriceAdjustment = 0,    IsDefault = true },
            new CreateAttributeValueRequest { Label = "L", PriceAdjustment = 5000, IsDefault = false },
          ],
        },
      ],
    };

    Response(201, "Sản phẩm được tạo thành công. Trả về Id của sản phẩm mới.");
    Response(400, "Dữ liệu không hợp lệ.");
    Response(404, "CategoryId không tồn tại.");
  }
}
