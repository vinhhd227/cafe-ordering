using Api.UseCases.Orders.Split;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class SplitOrderRequest
{
  public int Id { get; set; }
  public IReadOnlyList<SplitItemRequestDto> Items { get; set; } = [];
}

public sealed class SplitItemRequestDto
{
  public int ProductId { get; set; }
  public int Quantity { get; set; }
}

public class SplitOrder(IMediator mediator) : Endpoint<SplitOrderRequest, SplitOrderResult>
{
  public override void Configure()
  {
    Post("/api/admin/orders/{id}/split");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(SplitOrderRequest req, CancellationToken ct)
  {
    var items = req.Items
      .Select(i => new SplitItemRequest(i.ProductId, i.Quantity))
      .ToList();

    var result = await mediator.Send(new SplitOrderCommand(req.Id, items), ct);
    await this.SendResultAsync(result, ct);
  }
}
