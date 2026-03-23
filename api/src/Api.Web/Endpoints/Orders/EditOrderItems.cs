using Api.UseCases.Orders.Edit;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class EditOrderItemsRequest
{
  public int Id { get; set; }
  public IReadOnlyList<EditOrderItemRequestDto> Items { get; set; } = [];
  public int? GuestCount { get; set; }
}

public sealed class EditOrderItemRequestDto
{
  public int ProductId { get; set; }
  public int Quantity { get; set; }
  public string? Temperature { get; set; }
  public string? IceLevel { get; set; }
  public string? SugarLevel { get; set; }
  public bool IsTakeaway { get; set; }
  public string? Note { get; set; }
}

public class EditOrderItems(IMediator mediator) : Endpoint<EditOrderItemsRequest>
{
  public override void Configure()
  {
    Put("/api/admin/orders/{id}/items");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(EditOrderItemsRequest req, CancellationToken ct)
  {
    var command = new EditOrderItemsCommand(
      req.Id,
      req.Items.Select(i => new EditOrderItemDto(
        i.ProductId, i.Quantity,
        i.Temperature, i.IceLevel, i.SugarLevel,
        i.IsTakeaway, i.Note)).ToList(),
      req.GuestCount);

    var result = await mediator.Send(command, ct);
    await this.SendResultAsync(result, ct);
  }
}
