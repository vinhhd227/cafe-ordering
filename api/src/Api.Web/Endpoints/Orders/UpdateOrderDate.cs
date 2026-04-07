using Api.UseCases.Orders.UpdateOrderDate;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class UpdateOrderDateRequest
{
  public int Id { get; set; }
  public DateTime OrderDate { get; set; }
}

public class UpdateOrderDate(IMediator mediator) : Endpoint<UpdateOrderDateRequest>
{
  public override void Configure()
  {
    Patch("/api/admin/orders/{id}/order-date");
    Policies("admin.access", "order.update");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(UpdateOrderDateRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new UpdateOrderDateCommand(req.Id, req.OrderDate), ct);
    await this.SendResultAsync(result, ct);
  }
}
