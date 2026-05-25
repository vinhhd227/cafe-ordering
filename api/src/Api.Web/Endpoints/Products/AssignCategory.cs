using Api.UseCases.Products.AssignCategory;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public sealed class AssignCategoryRequest
{
  public int ProductId { get; set; }
  public int CategoryId { get; set; }
}

public class AssignCategoryEndpoint(IMediator mediator) : Endpoint<AssignCategoryRequest>
{
  public override void Configure()
  {
    Patch("/api/products/{ProductId}/assign-category");
    Policies("product.update");
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(AssignCategoryRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new AssignCategoryCommand(req.ProductId, req.CategoryId), ct);
    await this.SendResultAsync(result, ct);
  }
}

public class AssignCategorySummary : Summary<AssignCategoryEndpoint>
{
  public AssignCategorySummary()
  {
    Summary = "Gán sản phẩm vào danh mục";
    Description = "Gán sản phẩm vào một danh mục cụ thể.";
    Response(204, "Gán thành công");
    Response(404, "Không tìm thấy sản phẩm hoặc danh mục");
    Response(401, "Chưa xác thực");
    Response(403, "Không có quyền");
  }
}
