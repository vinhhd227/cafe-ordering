using Api.UseCases.Orders.DTOs;
using Api.UseCases.Orders.UpdateManual;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class UpdateManualOrderRequest
{
  public int Id { get; set; }
  public List<ManualOrderItemRequest> Items { get; set; } = [];
  public DateTime? OrderedAt { get; set; }
  public int? GuestCount { get; set; }
  public string Status { get; set; } = "PENDING";
  public string PaymentStatus { get; set; } = "UNPAID";
  public string PaymentMethod { get; set; } = "UNKNOWN";
  public decimal? AmountReceived { get; set; }
  public decimal TipAmount { get; set; }
}

public class UpdateManualOrder(IMediator mediator) : Endpoint<UpdateManualOrderRequest, OrderDto>
{
  public override void Configure()
  {
    Put("/api/admin/orders/{id}");
    Policies("admin.access", "order.update");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(UpdateManualOrderRequest req, CancellationToken ct)
  {
    var command = new UpdateManualOrderCommand(
      req.Id,
      req.Items.Select(i => new ManualOrderItemDto(
        i.ProductId, i.Quantity,
        i.Temperature, i.IceLevel, i.SugarLevel,
        i.IsTakeaway, i.Note)).ToList(),
      req.OrderedAt,
      req.GuestCount,
      req.Status,
      req.PaymentStatus,
      req.PaymentMethod,
      req.AmountReceived,
      req.TipAmount);

    var result = await mediator.Send(command, ct);
    await this.SendResultAsync(result, ct);
  }
}
