using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.UseCases.Orders.UpdateStatus;
using Api.UseCases.Sessions.AutoClose;
using IMediator = Mediator.IMediator;

namespace Api.UnitTests.UseCases.Orders;

public class UpdateOrderStatusHandlerTests
{
  private readonly IRepositoryBase<Order> _repo = Substitute.For<IRepositoryBase<Order>>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly UpdateOrderStatusHandler _handler;

  private static readonly Guid SessionId = Guid.NewGuid();

  public UpdateOrderStatusHandlerTests()
  {
    _handler = new UpdateOrderStatusHandler(_repo, _mediator);
  }

  [Fact]
  public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
  {
    _repo.FirstOrDefaultAsync(Arg.Any<OrderByIdWithItemsSpec>(), Arg.Any<CancellationToken>())
         .Returns((Order?)null);

    var result = await _handler.Handle(new UpdateOrderStatusCommand(99, "PROCESSING"), default);

    result.Status.Should().Be(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_WhenInvalidStatus_ReturnsInvalid()
  {
    var order = Order.Create(SessionId, "ORD-001");
    _repo.FirstOrDefaultAsync(Arg.Any<OrderByIdWithItemsSpec>(), Arg.Any<CancellationToken>())
         .Returns(order);

    var result = await _handler.Handle(new UpdateOrderStatusCommand(1, "INVALID_STATUS"), default);

    result.Status.Should().Be(ResultStatus.Invalid);
  }

  [Fact]
  public async Task Handle_WhenValidTransitionToProcessing_UpdatesAndSaves()
  {
    var order = Order.Create(SessionId, "ORD-001");
    _repo.FirstOrDefaultAsync(Arg.Any<OrderByIdWithItemsSpec>(), Arg.Any<CancellationToken>())
         .Returns(order);

    var result = await _handler.Handle(new UpdateOrderStatusCommand(1, "PROCESSING"), default);

    result.IsSuccess.Should().BeTrue();
    order.Status.Should().Be(OrderStatus.Processing);
    await _repo.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenInvalidTransition_ReturnsConflict()
  {
    var order = Order.Create(SessionId, "ORD-001");
    order.Process();
    order.Complete();
    _repo.FirstOrDefaultAsync(Arg.Any<OrderByIdWithItemsSpec>(), Arg.Any<CancellationToken>())
         .Returns(order);

    // Cannot go back to Processing from Completed
    var result = await _handler.Handle(new UpdateOrderStatusCommand(1, "PROCESSING"), default);

    result.Status.Should().Be(ResultStatus.Conflict);
  }

  [Fact]
  public async Task Handle_WhenCancelled_TriggersAutoClose()
  {
    var order = Order.Create(SessionId, "ORD-001");
    _repo.FirstOrDefaultAsync(Arg.Any<OrderByIdWithItemsSpec>(), Arg.Any<CancellationToken>())
         .Returns(order);

    await _handler.Handle(new UpdateOrderStatusCommand(1, "CANCELLED"), default);

    await _mediator.Received(1).Send(
      Arg.Is<TryAutoCloseSessionCommand>(c => c.SessionId == SessionId),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenNotCancelled_DoesNotTriggerAutoClose()
  {
    var order = Order.Create(SessionId, "ORD-001");
    _repo.FirstOrDefaultAsync(Arg.Any<OrderByIdWithItemsSpec>(), Arg.Any<CancellationToken>())
         .Returns(order);

    await _handler.Handle(new UpdateOrderStatusCommand(1, "PROCESSING"), default);

    await _mediator.DidNotReceive().Send(
      Arg.Any<TryAutoCloseSessionCommand>(),
      Arg.Any<CancellationToken>());
  }
}
