using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.Create;

public record CreateTableCommand(string Code) : ICommand<Result<TableDto>>;
