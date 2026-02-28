namespace Api.UseCases.Sessions.CreateCounter;

public record CreateCounterSessionCommand : ICommand<Result<Guid>>;
