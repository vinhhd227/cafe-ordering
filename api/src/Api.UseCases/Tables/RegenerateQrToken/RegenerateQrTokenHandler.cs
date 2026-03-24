using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.RegenerateQrToken;

public class RegenerateQrTokenHandler(IRepositoryBase<Table> repository)
  : ICommandHandler<RegenerateQrTokenCommand, Result<TableDto>>
{
  public async ValueTask<Result<TableDto>> Handle(RegenerateQrTokenCommand request, CancellationToken ct)
  {
    var table = await repository.FirstOrDefaultAsync(new TableByIdSpec(request.TableId), ct);

    if (table is null)
      return Result.NotFound($"Table {request.TableId} not found.");

    table.RegenerateQrToken();
    await repository.UpdateAsync(table, ct);

    return Result.Success(new TableDto(
      table.Id, table.Code, table.IsActive, table.Status.ToString(),
      table.ActiveSessionId, table.ZoneId, table.Zone?.Name, table.QrToken));
  }
}
