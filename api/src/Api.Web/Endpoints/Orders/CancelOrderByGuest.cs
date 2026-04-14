using Api.UseCases.Orders.CancelByGuest;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class CancelOrderByGuestRequest
{
  public int  OrderId   { get; set; }
  public Guid SessionId { get; set; }
}

public class CancelOrderByGuest(IMediator mediator) : Endpoint<CancelOrderByGuestRequest>
{
  public override void Configure()
  {
    Put("/api/orders/{orderId}/cancel");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(CancelOrderByGuestRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new CancelOrderByGuestCommand(req.OrderId, req.SessionId), ct);
    await this.SendResultAsync(result, ct);
  }
}
