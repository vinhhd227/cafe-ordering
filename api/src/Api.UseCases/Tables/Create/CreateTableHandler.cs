using Api.Core.Aggregates.TableAggregate;
using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.Create;

public class CreateTableHandler(IRepositoryBase<Table> repository)
  : ICommandHandler<CreateTableCommand, Result<TableDto>>
{
  public async ValueTask<Result<TableDto>> Handle(CreateTableCommand request, CancellationToken ct)
  {
    if (request.Number <= 0)
      return Result.Invalid(new ValidationError("Number", "Table number must be greater than zero."));

    if (string.IsNullOrWhiteSpace(request.Code))
      return Result.Invalid(new ValidationError("Code", "Table code is required."));

    var table = Table.Create(request.Number, request.Code);
    await repository.AddAsync(table, ct);

    return Result.Success(new TableDto(table.Id, table.Number, table.Code, table.IsActive, table.Status.ToString(), null));
  }
}
