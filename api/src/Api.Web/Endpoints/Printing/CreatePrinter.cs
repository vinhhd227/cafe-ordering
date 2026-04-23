using Api.UseCases.Printing.Create;
using Api.UseCases.Printing.DTOs;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Printing;

public sealed class CreatePrinterRequest
{
  public string Name             { get; set; } = string.Empty;
  public string Role             { get; set; } = string.Empty;
  public string FormatterType    { get; set; } = string.Empty;
  public string TransportType    { get; set; } = string.Empty;
  public string ConnectionParams { get; set; } = string.Empty;
  public int    PaperWidthMm    { get; set; }
}

public class CreatePrinter(IMediator mediator) : Endpoint<CreatePrinterRequest, PrinterConfigDto>
{
  public override void Configure()
  {
    Post("/api/admin/printers");
    Policies("printer.create");
    DontAutoTag();
    Description(b => b.WithTags("Printing"));
  }

  public override async Task HandleAsync(CreatePrinterRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new CreatePrinterConfigCommand(
      req.Name, req.Role, req.FormatterType, req.TransportType, req.ConnectionParams, req.PaperWidthMm), ct);
    await this.SendResultAsync(result, ct);
  }
}
