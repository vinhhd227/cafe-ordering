using Api.UseCases.Products.UnassignCategory;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public sealed class UnassignCategoryRequest
{
  public int ProductId { get; set; }
}

public class UnassignCategoryEndpoint(IMediator mediator) : Endpoint<UnassignCategoryRequest>
{
  public override void Configure()
  {
    Patch("/api/products/{ProductId}/unassign-category");
    Policies("product.update");
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(UnassignCategoryRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new UnassignCategoryCommand(req.ProductId), ct);
    await this.SendResultAsync(result, ct);
  }
}

public class UnassignCategorySummary : Summary<UnassignCategoryEndpoint>
{
  public UnassignCategorySummary()
  {
    Summary = "Gỡ sản phẩm khỏi danh mục";
    Description = "Xóa liên kết danh mục của sản phẩm (set CategoryId = null).";
    Response(204, "Gỡ thành công");
    Response(404, "Không tìm thấy sản phẩm");
    Response(401, "Chưa xác thực");
    Response(403, "Không có quyền");
  }
}
