using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Sessions.DTOs;

namespace Api.UseCases.Sessions.GetOrCreate;

public class GetOrCreateSessionHandler(
  IRepositoryBase<Table> tableRepository,
  IRepositoryBase<GuestSession> sessionRepository)
  : ICommandHandler<GetOrCreateSessionCommand, Result<SessionContextDto>>
{
  public async ValueTask<Result<SessionContextDto>> Handle(
    GetOrCreateSessionCommand request, CancellationToken ct)
  {
    var tableSpec = new TableByIdSpec(request.TableId);
    var table = await tableRepository.FirstOrDefaultAsync(tableSpec, ct);

    if (table is null)
      return Result.NotFound($"Table {request.TableId} not found.");

    if (!table.IsActive)
      return Result.Error($"Table {request.TableId} is inactive.");

    // Return existing active session if one exists
    var activeSessionSpec = new ActiveSessionByTableIdSpec(request.TableId);
    var existingSession = await sessionRepository.FirstOrDefaultAsync(activeSessionSpec, ct);

    if (existingSession is not null)
    {
      return Result.Success(new SessionContextDto(
        existingSession.Id,
        existingSession.TableId,
        existingSession.Status));
    }

    // If the table is still marked Occupied but no active session exists, the state is
    // inconsistent (e.g. the session was closed without the table being reset).
    // Reset the table so we can safely open a fresh session below.
    if (table.Status == TableStatus.Occupied)
      table.MarkAvailable();

    // Create a new session and open the table
    var session = GuestSession.Create(request.TableId);
    table.OpenSession(session.Id);

    await sessionRepository.AddAsync(session, ct);
    await tableRepository.UpdateAsync(table, ct);

    return Result.Success(new SessionContextDto(session.Id, session.TableId, session.Status));
  }
}
