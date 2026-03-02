using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.Create;

public class CreateTableHandler(IRepositoryBase<Table> repository)
  : ICommandHandler<CreateTableCommand, Result<TableDto>>
{
  public async ValueTask<Result<TableDto>> Handle(CreateTableCommand request, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(request.Code))
      return Result.Invalid(new ValidationError("Code", "Table code is required."));

    var existing = await repository.FirstOrDefaultAsync(new TableByCodeSpec(request.Code), ct);
    if (existing is not null)
      return Result.Invalid(new ValidationError("Code", $"Table code '{request.Code}' already exists."));

    var table = Table.Create(request.Code);
    await repository.AddAsync(table, ct);

    return Result.Success(new TableDto(table.Id, table.Code, table.IsActive, table.Status.ToString(), null));
  }
}
