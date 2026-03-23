using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.Update;

public record UpdateTableCommand(int TableId, string Code, int? ZoneId = null) : ICommand<Result<TableDto>>;
