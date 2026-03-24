using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.RegenerateQrToken;

public record RegenerateQrTokenCommand(int TableId) : ICommand<Result<TableDto>>;
