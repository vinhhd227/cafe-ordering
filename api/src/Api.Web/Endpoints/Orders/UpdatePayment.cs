using Api.UseCases.Orders.UpdatePayment;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class UpdatePaymentRequest
{
  public int Id { get; set; }
  public string PaymentStatus { get; set; } = string.Empty;
  public string PaymentMethod { get; set; } = string.Empty;
  public decimal? AmountReceived { get; set; }
  public decimal TipAmount { get; set; }
}

public class UpdatePayment(IMediator mediator) : Endpoint<UpdatePaymentRequest>
{
  public override void Configure()
  {
    Patch("/api/admin/orders/{id}/payment");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(UpdatePaymentRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new UpdatePaymentCommand(req.Id, req.PaymentStatus, req.PaymentMethod, req.AmountReceived, req.TipAmount), ct);
    await this.SendResultAsync(result, ct);
  }
}
