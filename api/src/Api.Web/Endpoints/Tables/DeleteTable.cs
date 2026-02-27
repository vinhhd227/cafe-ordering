using System.Security.Claims;
using Api.UseCases.Tables.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Tables;

public sealed class DeleteTableRequest
{
  public int TableId { get; set; }
}

public class DeleteTable(IMediator mediator) : Ep.Req<DeleteTableRequest>.NoRes
{
  public override void Configure()
  {
    Delete("/api/admin/tables/{TableId}");
    Policies("table.delete");
    DontAutoTag();
    Description(b => b.WithTags("Tables"));
  }

  public override async Task HandleAsync(DeleteTableRequest req, CancellationToken ct)
  {
    var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "system";
    var result = await mediator.Send(new DeleteTableCommand(req.TableId, deletedBy), ct);
    await this.SendResultAsync(result, ct);
  }
}
