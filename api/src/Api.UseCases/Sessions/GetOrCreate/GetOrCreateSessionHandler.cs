using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Sessions.DTOs;
using Microsoft.Extensions.Configuration;

namespace Api.UseCases.Sessions.GetOrCreate;

public class GetOrCreateSessionHandler(
  IRepositoryBase<Table> tableRepository,
  IRepositoryBase<GuestSession> sessionRepository,
  IReadRepositoryBase<Order> orderRepository,
  IConfiguration configuration)
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

    var enforce = configuration.GetValue<bool>("QrEnforcementEnabled");
    if (enforce &&
        (string.IsNullOrWhiteSpace(request.Token) ||
         !string.Equals(request.Token, table.QrToken.ToString(), StringComparison.OrdinalIgnoreCase)))
    {
      return Result.Forbidden();
    }

    // Tìm session active hiện tại của bàn
    var activeSessionSpec = new ActiveSessionByTableIdSpec(request.TableId);
    var existingSession = await sessionRepository.FirstOrDefaultAsync(activeSessionSpec, ct);

    if (existingSession is not null)
    {
      // Kiểm tra session còn order PENDING/PROCESSING không
      var hasActiveOrders = await orderRepository.AnyAsync(
        new ActiveOrdersBySessionIdSpec(existingSession.Id), ct);

      if (hasActiveOrders)
      {
        // Session còn đang phục vụ → reuse, sync table nếu cần
        if (table.ActiveSessionId != existingSession.Id)
        {
          table.SyncActiveSession(existingSession.Id);
          await tableRepository.UpdateAsync(table, ct);
        }

        return Result.Success(new SessionContextDto(
          existingSession.Id,
          existingSession.TableId,
          existingSession.Status));
      }

      // Session không còn order active → đóng lại, tạo session mới cho khách tiếp theo
      existingSession.Close();
      await sessionRepository.UpdateAsync(existingSession, ct);
      table.MarkAvailable();
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
