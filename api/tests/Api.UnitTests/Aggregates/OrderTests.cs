using System.Reflection;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate;

namespace Api.UnitTests.Aggregates;

public class OrderTests
{
  private static readonly Guid SessionId = Guid.NewGuid();

  /// <summary>
  ///   In production, EF Core assigns a non-zero Id before AddItem is called.
  ///   This helper sets the Id via reflection to simulate that for unit tests.
  /// </summary>
  private static Order CreateOrderWithId(int id = 1)
  {
    var order = Order.Create(SessionId, "ORD-001");
    typeof(Order).GetProperty("Id")!.GetSetMethod(nonPublic: true)!.Invoke(order, [id]);
    return order;
  }

  [Fact]
  public void Create_ShouldSetPendingStatusAndUnpaidPayment()
  {
    var order = Order.Create(SessionId, "ORD-001");

    order.Status.Should().Be(OrderStatus.Pending);
    order.PaymentStatus.Should().Be(PaymentStatus.Unpaid);
    order.PaymentMethod.Should().Be(PaymentMethod.Unknown);
  }

  [Fact]
  public void AddItem_WhenPending_ShouldAddItem()
  {
    var order = CreateOrderWithId();

    order.AddItem(1, "Cà phê sữa", 30000m, 2);

    order.Items.Should().HaveCount(1);
    order.Items.First().Quantity.Should().Be(2);
    order.TotalAmount.Should().Be(60000m);
  }

  [Fact]
  public void AddItem_WhenCompleted_ShouldThrow()
  {
    var order = CreateOrderWithId();
    order.Process();
    order.Complete();

    var act = () => order.AddItem(1, "Cà phê sữa", 30000m, 1);

    act.Should().Throw<InvalidOperationException>()
       .WithMessage("*completed*");
  }

  [Fact]
  public void Process_WhenPending_ShouldTransitionToProcessing()
  {
    var order = Order.Create(SessionId, "ORD-001");

    order.Process();

    order.Status.Should().Be(OrderStatus.Processing);
  }

  [Fact]
  public void Process_WhenCompleted_ShouldThrow()
  {
    var order = Order.Create(SessionId, "ORD-001");
    order.Process();
    order.Complete();

    var act = () => order.Process();

    act.Should().Throw<InvalidOperationException>();
  }

  [Fact]
  public void Complete_WhenProcessing_ShouldTransitionAndFireEvent()
  {
    var order = Order.Create(SessionId, "ORD-001");
    order.Process();
    order.ClearDomainEvents();

    order.Complete();

    order.Status.Should().Be(OrderStatus.Completed);
    order.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "OrderCompletedEvent");
  }

  [Fact]
  public void Cancel_WhenPending_ShouldChangeToCancelled()
  {
    var order = Order.Create(SessionId, "ORD-001");

    order.Cancel();

    order.Status.Should().Be(OrderStatus.Cancelled);
  }

  [Fact]
  public void Cancel_WhenCompleted_ShouldThrow()
  {
    var order = Order.Create(SessionId, "ORD-001");
    order.Process();
    order.Complete();

    var act = () => order.Cancel();

    act.Should().Throw<InvalidOperationException>();
  }

  [Fact]
  public void UpdatePayment_WhenPaidWithCash_ShouldUpdate()
  {
    var order = Order.Create(SessionId, "ORD-001");

    order.UpdatePayment(PaymentStatus.Paid, PaymentMethod.Cash, amountReceived: 50000m, tipAmount: 0);

    order.PaymentStatus.Should().Be(PaymentStatus.Paid);
    order.PaymentMethod.Should().Be(PaymentMethod.Cash);
    order.AmountReceived.Should().Be(50000m);
  }

  [Fact]
  public void UpdatePayment_WhenPaidWithUnknownMethod_ShouldThrow()
  {
    var order = Order.Create(SessionId, "ORD-001");

    var act = () => order.UpdatePayment(PaymentStatus.Paid, PaymentMethod.Unknown);

    act.Should().Throw<InvalidOperationException>()
       .WithMessage("*PaymentMethod is required*");
  }
}
