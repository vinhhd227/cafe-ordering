using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.Create;

public record CreateTableCommand(int Number, string Code) : ICommand<Result<TableDto>>;
