using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;

namespace Api.UseCases.Sessions.Close;

public class CloseSessionHandler(
  IRepositoryBase<GuestSession> sessionRepository,
  IRepositoryBase<Table> tableRepository)
  : ICommandHandler<CloseSessionCommand, Result>
{
  public async ValueTask<Result> Handle(CloseSessionCommand request, CancellationToken ct)
  {
    var sessionSpec = new SessionByIdSpec(request.SessionId);
    var session = await sessionRepository.FirstOrDefaultAsync(sessionSpec, ct);

    if (session is null)
      return Result.NotFound($"Session {request.SessionId} not found.");

    if (session.Status == GuestSessionStatus.Closed)
      return Result.Conflict("Session is already closed.");

    session.Close();
    await sessionRepository.UpdateAsync(session, ct);

    var tableSpec = new TableByIdSpec(session.TableId);
    var table = await tableRepository.FirstOrDefaultAsync(tableSpec, ct);

    if (table is not null)
    {
      table.CloseSession();
      await tableRepository.UpdateAsync(table, ct);
    }

    return Result.Success();
  }
}
