using Api.UseCases.Printing.DTOs;
using Api.UseCases.Printing.PrintBill;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Printing;

public sealed class PrintBillRequest
{
  public int  OrderId   { get; set; }
  public int? PrinterId { get; set; }
}

public class PrintBillEndpoint(IMediator mediator) : Endpoint<PrintBillRequest, PrintLabelsResultDto>
{
  public override void Configure()
  {
    Post("/api/admin/print/bill");
    Policies("order.print");
    DontAutoTag();
    Description(b => b.WithTags("Printing"));
  }

  public override async Task HandleAsync(PrintBillRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new PrintBillCommand(req.OrderId, req.PrinterId), ct);
    await this.SendResultAsync(result, ct);
  }
}
