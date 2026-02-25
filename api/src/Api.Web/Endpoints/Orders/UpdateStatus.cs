using Api.UseCases.Orders.UpdateStatus;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class UpdateOrderStatusRequest
{
  public int Id { get; set; }
  public string Status { get; set; } = string.Empty;
}

public class UpdateOrderStatus(IMediator mediator) : Endpoint<UpdateOrderStatusRequest>
{
  public override void Configure()
  {
    Patch("/api/admin/orders/{id}/status");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(UpdateOrderStatusRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new UpdateOrderStatusCommand(req.Id, req.Status), ct);
    await this.SendResultAsync(result, ct);
  }
}
