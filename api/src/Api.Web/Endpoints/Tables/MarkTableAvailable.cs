using Api.UseCases.Tables.MarkAvailable;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Tables;

public sealed class MarkTableAvailableRequest
{
  /// <summary>The integer ID of the table.</summary>
  public int TableId { get; set; }
}

public class MarkTableAvailable(IMediator mediator) : Endpoint<MarkTableAvailableRequest>
{
  public override void Configure()
  {
    Put("/api/tables/{TableId}/available");
    Roles("Barista", "Manager", "Admin");
    DontAutoTag();
    Description(b => b.WithTags("Tables"));
  }

  public override async Task HandleAsync(MarkTableAvailableRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new MarkTableAvailableCommand(req.TableId), ct);
    await this.SendResultAsync(result, ct);
  }
}
