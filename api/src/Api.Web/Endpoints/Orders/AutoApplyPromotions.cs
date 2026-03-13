using Api.UseCases.Orders.AutoApplyPromotions;
using Api.UseCases.Orders.DTOs;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class AutoApplyPromotionsRequest
{
  public int OrderId { get; set; }
}

public class AutoApplyPromotions(IMediator mediator) : Endpoint<AutoApplyPromotionsRequest, OrderDto>
{
  public override void Configure()
  {
    Post("/api/admin/orders/{orderId}/promotions/auto");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(AutoApplyPromotionsRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new AutoApplyPromotionsCommand(req.OrderId), ct);
    await this.SendResultAsync(result, ct);
  }
}
