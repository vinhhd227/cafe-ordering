using Api.UseCases.Orders.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class DeleteOrderRequest
{
  public int Id { get; set; }
}

public class DeleteOrder(IMediator mediator) : Ep.Req<DeleteOrderRequest>.NoRes
{
  public override void Configure()
  {
    Delete("/api/admin/orders/{id}");
    Policies("admin.access", "order.delete");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(DeleteOrderRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new DeleteOrderCommand(req.Id), ct);
    await this.SendResultAsync(result, ct);
  }
}
