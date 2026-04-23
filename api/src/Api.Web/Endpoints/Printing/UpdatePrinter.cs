using Api.UseCases.Printing.DTOs;
using Api.UseCases.Printing.Update;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Printing;

public sealed class UpdatePrinterRequest
{
  public int    Id               { get; set; }
  public string Name             { get; set; } = string.Empty;
  public string FormatterType    { get; set; } = string.Empty;
  public string TransportType    { get; set; } = string.Empty;
  public string ConnectionParams { get; set; } = string.Empty;
  public int    PaperWidthMm    { get; set; }
}

public class UpdatePrinter(IMediator mediator) : Endpoint<UpdatePrinterRequest, PrinterConfigDto>
{
  public override void Configure()
  {
    Put("/api/admin/printers/{Id}");
    Policies("printer.update");
    DontAutoTag();
    Description(b => b.WithTags("Printing"));
  }

  public override async Task HandleAsync(UpdatePrinterRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new UpdatePrinterConfigCommand(
      req.Id, req.Name, req.FormatterType, req.TransportType, req.ConnectionParams, req.PaperWidthMm), ct);
    await this.SendResultAsync(result, ct);
  }
}
