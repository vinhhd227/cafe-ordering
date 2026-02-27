using Api.UseCases.Tables.DTOs;
using Api.UseCases.Tables.Update;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Tables;

public sealed class UpdateTableRequest
{
  public int TableId { get; set; }
  public int Number { get; set; }
  public string Code { get; set; } = string.Empty;
}

public class UpdateTable(IMediator mediator) : Endpoint<UpdateTableRequest, TableDto>
{
  public override void Configure()
  {
    Put("/api/admin/tables/{TableId}");
    Policies("table.update");
    DontAutoTag();
    Description(b => b.WithTags("Tables"));
  }

  public override async Task HandleAsync(UpdateTableRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new UpdateTableCommand(req.TableId, req.Number, req.Code), ct);
    await this.SendResultAsync(result, ct);
  }
}
