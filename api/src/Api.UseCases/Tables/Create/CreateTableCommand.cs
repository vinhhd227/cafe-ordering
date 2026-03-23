using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.Create;

public record CreateTableCommand(string Code, int? ZoneId = null) : ICommand<Result<TableDto>>;
