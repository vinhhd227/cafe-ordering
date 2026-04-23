using System.Security.Claims;
using Api.UseCases.Printing.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Printing;

public sealed class DeletePrinterRequest
{
  public int Id { get; set; }
}

public class DeletePrinter(IMediator mediator) : Ep.Req<DeletePrinterRequest>.NoRes
{
  public override void Configure()
  {
    Delete("/api/admin/printers/{Id}");
    Policies("printer.delete");
    DontAutoTag();
    Description(b => b.WithTags("Printing"));
  }

  public override async Task HandleAsync(DeletePrinterRequest req, CancellationToken ct)
  {
    var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "system";
    var result = await mediator.Send(new DeletePrinterConfigCommand(req.Id, deletedBy), ct);
    await this.SendResultAsync(result, ct);
  }
}
