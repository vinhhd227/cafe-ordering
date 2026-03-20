using Api.UseCases.Tables.DTOs;
using Api.UseCases.Tables.ListPublic;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Tables;

public class ListPublicTables(IMediator mediator) : EndpointWithoutRequest<List<PublicTableDto>>
{
  public override void Configure()
  {
    Get("/api/tables");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Tables"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var result = await mediator.Send(new ListPublicTablesQuery(), ct);
    await this.SendResultAsync(result, ct);
  }
}
