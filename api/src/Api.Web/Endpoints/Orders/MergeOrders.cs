using Api.UseCases.Orders.Merge;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class MergeOrdersRequest
{
  public int PrimaryOrderId { get; set; }
  public IReadOnlyList<int> SecondaryOrderIds { get; set; } = [];
}

public class MergeOrders(IMediator mediator) : Endpoint<MergeOrdersRequest>
{
  public override void Configure()
  {
    Post("/api/admin/orders/merge");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(MergeOrdersRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new MergeOrdersCommand(req.PrimaryOrderId, req.SecondaryOrderIds), ct);
    await this.SendResultAsync(result, ct);
  }
}
