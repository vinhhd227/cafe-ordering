using Api.UseCases.Tables.Create;
using Api.UseCases.Tables.DTOs;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Tables;

public sealed class CreateTableRequest
{
  public int Number { get; set; }
  public string Code { get; set; } = string.Empty;
}

public class CreateTable(IMediator mediator) : Endpoint<CreateTableRequest, TableDto>
{
  public override void Configure()
  {
    Post("/api/admin/tables");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Tables"));
  }

  public override async Task HandleAsync(CreateTableRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new CreateTableCommand(req.Number, req.Code), ct);
    await this.SendResultAsync(result, ct);
  }
}
