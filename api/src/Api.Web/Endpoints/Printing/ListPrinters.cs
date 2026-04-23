using Api.UseCases.Printing.DTOs;
using Api.UseCases.Printing.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Printing;

public class ListPrinters(IMediator mediator) : EndpointWithoutRequest<List<PrinterConfigDto>>
{
  public override void Configure()
  {
    Get("/api/admin/printers");
    Policies("printer.read");
    DontAutoTag();
    Description(b => b.WithTags("Printing"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var result = await mediator.Send(new ListPrinterConfigsQuery(), ct);
    await this.SendResultAsync(result, ct);
  }
}
