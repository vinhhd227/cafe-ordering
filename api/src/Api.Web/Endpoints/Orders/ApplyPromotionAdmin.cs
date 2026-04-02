using Api.UseCases.Orders.DTOs;
using Api.UseCases.Promotions.Apply;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class ApplyPromotionAdminRequest
{
  public int OrderId { get; set; }
  public string? Code { get; set; }
  public int? PromotionId { get; set; }
}

public class ApplyPromotionAdmin(IMediator mediator) : Endpoint<ApplyPromotionAdminRequest, OrderDto>
{
  public override void Configure()
  {
    Post("/api/admin/orders/{orderId}/promotions");
    Policies("admin.access", "order.update");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(ApplyPromotionAdminRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new ApplyPromotionCommand(req.OrderId, req.Code, req.PromotionId, null), ct);
    await this.SendResultAsync(result, ct);
  }
}
