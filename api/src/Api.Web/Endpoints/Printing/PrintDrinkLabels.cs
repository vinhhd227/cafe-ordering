using Api.UseCases.Printing.DTOs;
using Api.UseCases.Printing.PrintLabels;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Printing;

public sealed class PrintDrinkLabelsRequest
{
  public int    OrderId       { get; set; }
  public int[]? ItemIds       { get; set; }
  public int?   PrinterId     { get; set; }
  public string? Role         { get; set; }
  public int    CopiesPerItem { get; set; } = 1;
}

public class PrintDrinkLabels(IMediator mediator) : Endpoint<PrintDrinkLabelsRequest, PrintLabelsResultDto>
{
  public override void Configure()
  {
    Post("/api/admin/print/drink-labels");
    Policies("order.print");
    DontAutoTag();
    Description(b => b.WithTags("Printing"));
  }

  public override async Task HandleAsync(PrintDrinkLabelsRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new PrintDrinkLabelsCommand(
      req.OrderId, req.ItemIds, req.PrinterId, req.Role,
      req.CopiesPerItem > 0 ? req.CopiesPerItem : 1), ct);
    await this.SendResultAsync(result, ct);
  }
}
