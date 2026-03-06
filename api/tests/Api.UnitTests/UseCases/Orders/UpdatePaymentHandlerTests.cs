using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.UseCases.Orders.UpdatePayment;
using Api.UseCases.Sessions.AutoClose;
using IMediator = Mediator.IMediator;

namespace Api.UnitTests.UseCases.Orders;

public class UpdatePaymentHandlerTests
{
  private readonly IRepositoryBase<Order> _repo = Substitute.For<IRepositoryBase<Order>>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly UpdatePaymentHandler _handler;

  private static readonly Guid SessionId = Guid.NewGuid();

  public UpdatePaymentHandlerTests()
  {
    _handler = new UpdatePaymentHandler(_repo, _mediator);
  }

  [Fact]
  public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
  {
    _repo.FirstOrDefaultAsync(Arg.Any<OrderByIdWithItemsSpec>(), Arg.Any<CancellationToken>())
         .Returns((Order?)null);

    var result = await _handler.Handle(
      new UpdatePaymentCommand(99, "PAID", "CASH", null, 0), default);

    result.Status.Should().Be(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_WhenInvalidPaymentStatus_ReturnsInvalid()
  {
    var order = Order.Create(SessionId, "ORD-001");
    _repo.FirstOrDefaultAsync(Arg.Any<OrderByIdWithItemsSpec>(), Arg.Any<CancellationToken>())
         .Returns(order);

    var result = await _handler.Handle(
      new UpdatePaymentCommand(1, "INVALID", "CASH", null, 0), default);

    result.Status.Should().Be(ResultStatus.Invalid);
  }

  [Fact]
  public async Task Handle_WhenInvalidPaymentMethod_ReturnsInvalid()
  {
    var order = Order.Create(SessionId, "ORD-001");
    _repo.FirstOrDefaultAsync(Arg.Any<OrderByIdWithItemsSpec>(), Arg.Any<CancellationToken>())
         .Returns(order);

    var result = await _handler.Handle(
      new UpdatePaymentCommand(1, "PAID", "INVALID_METHOD", null, 0), default);

    result.Status.Should().Be(ResultStatus.Invalid);
  }

  [Fact]
  public async Task Handle_WhenPaidWithUnknownMethod_ReturnsConflict()
  {
    var order = Order.Create(SessionId, "ORD-001");
    _repo.FirstOrDefaultAsync(Arg.Any<OrderByIdWithItemsSpec>(), Arg.Any<CancellationToken>())
         .Returns(order);

    var result = await _handler.Handle(
      new UpdatePaymentCommand(1, "PAID", "UNKNOWN", null, 0), default);

    result.Status.Should().Be(ResultStatus.Conflict);
  }

  [Fact]
  public async Task Handle_WhenPaidWithCash_UpdatesPaymentAndTriggersAutoClose()
  {
    var order = Order.Create(SessionId, "ORD-001");
    _repo.FirstOrDefaultAsync(Arg.Any<OrderByIdWithItemsSpec>(), Arg.Any<CancellationToken>())
         .Returns(order);

    var result = await _handler.Handle(
      new UpdatePaymentCommand(1, "PAID", "CASH", 50000m, 0), default);

    result.IsSuccess.Should().BeTrue();
    order.PaymentStatus.Should().Be(PaymentStatus.Paid);
    order.PaymentMethod.Should().Be(PaymentMethod.Cash);
    await _repo.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
    await _mediator.Received(1).Send(
      Arg.Is<TryAutoCloseSessionCommand>(c => c.SessionId == SessionId),
      Arg.Any<CancellationToken>());
  }
}
