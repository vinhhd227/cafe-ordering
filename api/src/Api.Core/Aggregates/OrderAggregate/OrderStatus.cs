namespace Api.Core.Aggregates.OrderAggregate;

public class OrderStatus : SmartEnum<OrderStatus>
{
  public static readonly OrderStatus Pending = new PendingStatus();
  public static readonly OrderStatus Processing = new ProcessingStatus();
  public static readonly OrderStatus Completed = new CompletedStatus();
  public static readonly OrderStatus Cancelled = new CancelledStatus();

  private OrderStatus(string name, int value) : base(name, value) { }

  public virtual bool CanAddItems => false;
  public virtual bool CanCancel => false;

  // Business logic
  public virtual bool CanTransitionTo(OrderStatus nextStatus)
  {
    return false;
  }

  // Nested classes for each status
  private class PendingStatus : OrderStatus
  {
    public PendingStatus() : base(nameof(Pending), 1) { }

    public override bool CanAddItems => true;
    public override bool CanCancel => true;

    public override bool CanTransitionTo(OrderStatus nextStatus)
    {
      return nextStatus == Processing || nextStatus == Cancelled;
    }
  }

  private class ProcessingStatus : OrderStatus
  {
    public ProcessingStatus() : base(nameof(Processing), 2) { }

    public override bool CanAddItems => false;
    public override bool CanCancel => true;

    public override bool CanTransitionTo(OrderStatus nextStatus)
    {
      return nextStatus == Completed || nextStatus == Cancelled;
    }
  }

  private class CompletedStatus : OrderStatus
  {
    public CompletedStatus() : base(nameof(Completed), 3) { }

    public override bool CanAddItems => false;
    public override bool CanCancel => false;
  }

  private class CancelledStatus : OrderStatus
  {
    public CancelledStatus() : base(nameof(Cancelled), 4) { }

    public override bool CanAddItems => false;
    public override bool CanCancel => false;
  }
}
