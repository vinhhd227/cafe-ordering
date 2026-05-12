using Api.UseCases.ProductOptionGroups.UnlinkFromProduct;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.ProductOptionGroups;

public class UnlinkFromProductRequest
{
  public int GroupId { get; set; }
  public int ProductId { get; set; }
}

public class UnlinkFromProductSummary : Summary<UnlinkFromProductEndpoint>
{
  public UnlinkFromProductSummary()
  {
    Summary = "Gỡ liên kết sản phẩm khỏi nhóm bán kèm";
    Description = "Xóa liên kết giữa một sản phẩm và nhóm bán kèm.";
    Response(204, "Gỡ liên kết thành công");
    Response(404, "Không tìm thấy sản phẩm");
    Response(401, "Chưa xác thực");
    Response(403, "Không có quyền");
  }
}

public class UnlinkFromProductEndpoint(IMediator mediator) : Ep.Req<UnlinkFromProductRequest>.NoRes
{
  public override void Configure()
  {
    Delete("/api/product-option-groups/{GroupId}/linked-products/{ProductId}");
    Policies("product.update");
    DontAutoTag();
    Description(b => b.WithTags("ProductOptionGroups"));
  }

  public override async Task HandleAsync(UnlinkFromProductRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new UnlinkGroupFromProductCommand(req.GroupId, req.ProductId), ct);
    await this.SendResultAsync(result, ct);
  }
}
