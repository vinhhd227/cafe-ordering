using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.Core.Domain.Enums;
using Api.UseCases.Sessions.AutoClose;

namespace Api.UnitTests.UseCases.Sessions;

public class TryAutoCloseSessionHandlerTests
{
  private readonly IRepositoryBase<Order> _orderRepo = Substitute.For<IRepositoryBase<Order>>();
  private readonly IRepositoryBase<GuestSession> _sessionRepo = Substitute.For<IRepositoryBase<GuestSession>>();
  private readonly IRepositoryBase<Table> _tableRepo = Substitute.For<IRepositoryBase<Table>>();
  private readonly TryAutoCloseSessionHandler _handler;

  private static readonly Guid SessionId = Guid.NewGuid();

  public TryAutoCloseSessionHandlerTests()
  {
    _handler = new TryAutoCloseSessionHandler(_orderRepo, _sessionRepo, _tableRepo);
  }

  private static Order PaidOrder()
  {
    var order = Order.Create(SessionId, "ORD-001");
    order.UpdatePayment(PaymentStatus.Paid, PaymentMethod.Cash, amountReceived: 50000);
    return order;
  }

  private static Order CancelledOrder()
  {
    var order = Order.Create(SessionId, "ORD-002");
    order.Cancel();
    return order;
  }

  private static Order PendingOrder() => Order.Create(SessionId, "ORD-003");

  private static Order CompletedUnpaidOrder()
  {
    var order = Order.Create(SessionId, "ORD-004");
    order.Process();
    order.Complete();
    return order;
  }

  [Fact]
  public async Task AllOrdersPaid_ClosesSessionAndSetsTableCleaning()
  {
    var session = GuestSession.Create(tableId: 1);
    var table = Table.Create("F1-01");
    table.OpenSession(session.Id);

    _orderRepo.ListAsync(Arg.Any<OrdersBySessionIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(new List<Order> { PaidOrder(), PaidOrder() });
    _sessionRepo.FirstOrDefaultAsync(Arg.Any<SessionByIdSpec>(), Arg.Any<CancellationToken>())
                .Returns(session);
    _tableRepo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(table);

    var result = await _handler.Handle(new TryAutoCloseSessionCommand(SessionId), default);

    result.IsSuccess.Should().BeTrue();
    session.Status.Should().Be(GuestSessionStatus.Closed);
    table.Status.Should().Be(TableStatus.Cleaning);
    await _sessionRepo.Received(1).UpdateAsync(session, Arg.Any<CancellationToken>());
    await _tableRepo.Received(1).UpdateAsync(table, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task AllOrdersCancelled_ClosesSession()
  {
    var session = GuestSession.Create(tableId: 1);
    var table = Table.Create("F1-02");
    table.OpenSession(session.Id);

    _orderRepo.ListAsync(Arg.Any<OrdersBySessionIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(new List<Order> { CancelledOrder(), CancelledOrder() });
    _sessionRepo.FirstOrDefaultAsync(Arg.Any<SessionByIdSpec>(), Arg.Any<CancellationToken>())
                .Returns(session);
    _tableRepo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(table);

    var result = await _handler.Handle(new TryAutoCloseSessionCommand(SessionId), default);

    result.IsSuccess.Should().BeTrue();
    session.Status.Should().Be(GuestSessionStatus.Closed);
  }

  [Fact]
  public async Task MixedPaidAndCancelled_ClosesSession()
  {
    var session = GuestSession.Create(tableId: 1);
    var table = Table.Create("F1-03");
    table.OpenSession(session.Id);

    _orderRepo.ListAsync(Arg.Any<OrdersBySessionIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(new List<Order> { PaidOrder(), CancelledOrder() });
    _sessionRepo.FirstOrDefaultAsync(Arg.Any<SessionByIdSpec>(), Arg.Any<CancellationToken>())
                .Returns(session);
    _tableRepo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(table);

    var result = await _handler.Handle(new TryAutoCloseSessionCommand(SessionId), default);

    result.IsSuccess.Should().BeTrue();
    session.Status.Should().Be(GuestSessionStatus.Closed);
  }

  [Fact]
  public async Task OneOrderStillUnpaid_DoesNotCloseSession()
  {
    _orderRepo.ListAsync(Arg.Any<OrdersBySessionIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(new List<Order> { PaidOrder(), CompletedUnpaidOrder() });

    var result = await _handler.Handle(new TryAutoCloseSessionCommand(SessionId), default);

    result.IsSuccess.Should().BeTrue();
    await _sessionRepo.DidNotReceive().FirstOrDefaultAsync(Arg.Any<SessionByIdSpec>(), Arg.Any<CancellationToken>());
    await _sessionRepo.DidNotReceive().UpdateAsync(Arg.Any<GuestSession>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task AllOrdersPending_DoesNotCloseSession()
  {
    _orderRepo.ListAsync(Arg.Any<OrdersBySessionIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(new List<Order> { PendingOrder() });

    var result = await _handler.Handle(new TryAutoCloseSessionCommand(SessionId), default);

    result.IsSuccess.Should().BeTrue();
    await _sessionRepo.DidNotReceive().UpdateAsync(Arg.Any<GuestSession>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SessionAlreadyClosed_ReturnsSuccessWithoutUpdating()
  {
    var session = GuestSession.Create(tableId: 1);
    session.Close(); // already closed

    _orderRepo.ListAsync(Arg.Any<OrdersBySessionIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(new List<Order> { PaidOrder() });
    _sessionRepo.FirstOrDefaultAsync(Arg.Any<SessionByIdSpec>(), Arg.Any<CancellationToken>())
                .Returns(session);

    var result = await _handler.Handle(new TryAutoCloseSessionCommand(SessionId), default);

    result.IsSuccess.Should().BeTrue();
    await _sessionRepo.DidNotReceive().UpdateAsync(Arg.Any<GuestSession>(), Arg.Any<CancellationToken>());
    await _tableRepo.DidNotReceive().UpdateAsync(Arg.Any<Table>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task NoOrders_ReturnsSuccessWithoutTouchingSession()
  {
    _orderRepo.ListAsync(Arg.Any<OrdersBySessionIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(new List<Order>());

    var result = await _handler.Handle(new TryAutoCloseSessionCommand(SessionId), default);

    result.IsSuccess.Should().BeTrue();
    await _sessionRepo.DidNotReceive().FirstOrDefaultAsync(Arg.Any<SessionByIdSpec>(), Arg.Any<CancellationToken>());
    await _sessionRepo.DidNotReceive().UpdateAsync(Arg.Any<GuestSession>(), Arg.Any<CancellationToken>());
  }
}
