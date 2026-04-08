using Api.UseCases.Orders.CreateManual;
using Api.UseCases.Orders.DTOs;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class CreateManualOrderRequest
{
  public int TableId { get; set; }
  public List<ManualOrderItemRequest> Items { get; set; } = [];
  public DateTime? OrderedAt { get; set; }
  public int? GuestCount { get; set; }
  public string Status { get; set; } = "PENDING";
  public string PaymentStatus { get; set; } = "UNPAID";
  public string PaymentMethod { get; set; } = "UNKNOWN";
  public decimal? AmountReceived { get; set; }
  public decimal TipAmount { get; set; }
}

public sealed class ManualOrderItemRequest
{
  public int ProductId { get; set; }
  public int Quantity { get; set; }
  public string? Temperature { get; set; }
  public string? IceLevel { get; set; }
  public string? SugarLevel { get; set; }
  public bool IsTakeaway { get; set; }
  public string? Note { get; set; }
}

public class CreateManualOrder(IMediator mediator) : Endpoint<CreateManualOrderRequest, OrderDto>
{
  public override void Configure()
  {
    Post("/api/admin/orders");
    Policies("admin.access", "order.createManual");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(CreateManualOrderRequest req, CancellationToken ct)
  {
    var command = new CreateManualOrderCommand(
      req.TableId,
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
