using Api.UseCases.Sessions.DTOs;

namespace Api.UseCases.Sessions.GetOrCreate;

public record GetOrCreateSessionCommand(int TableId) : ICommand<Result<SessionContextDto>>;
