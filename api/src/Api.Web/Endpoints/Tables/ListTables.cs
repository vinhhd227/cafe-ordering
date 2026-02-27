using Api.UseCases.Tables.DTOs;
using Api.UseCases.Tables.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Tables;

public class ListTables(IMediator mediator) : EndpointWithoutRequest<List<TableDto>>
{
  public override void Configure()
  {
    Get("/api/admin/tables");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Tables"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var result = await mediator.Send(new ListTablesQuery(), ct);
    await this.SendResultAsync(result, ct);
  }
}
