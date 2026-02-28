using Api.Core.Aggregates.GuestSessionAggregate;

namespace Api.UseCases.Sessions.CreateCounter;

public class CreateCounterSessionHandler(
  IRepositoryBase<GuestSession> sessionRepository)
  : ICommandHandler<CreateCounterSessionCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(
    CreateCounterSessionCommand request, CancellationToken ct)
  {
    var session = GuestSession.CreateCounter();
    await sessionRepository.AddAsync(session, ct);
    return Result.Success(session.Id);
  }
}
