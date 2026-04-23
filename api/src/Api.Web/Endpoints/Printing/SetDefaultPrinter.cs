using Api.UseCases.Printing.SetDefault;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Printing;

public sealed class SetDefaultPrinterRequest
{
  public int Id { get; set; }
}

public class SetDefaultPrinter(IMediator mediator) : Ep.Req<SetDefaultPrinterRequest>.NoRes
{
  public override void Configure()
  {
    Put("/api/admin/printers/{Id}/set-default");
    Policies("printer.update");
    DontAutoTag();
    Description(b => b.WithTags("Printing"));
  }

  public override async Task HandleAsync(SetDefaultPrinterRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new SetDefaultPrinterCommand(req.Id), ct);
    await this.SendResultAsync(result, ct);
  }
}
