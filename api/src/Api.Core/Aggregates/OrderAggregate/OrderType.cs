using Ardalis.SmartEnum;

namespace Api.Core.Aggregates.OrderAggregate;

public class OrderType : SmartEnum<OrderType>
{
  public static readonly OrderType DineIn   = new DineInType();
  public static readonly OrderType Takeaway = new TakeawayType();
  public static readonly OrderType Delivery = new DeliveryType();

  private OrderType(string name, int value) : base(name, value) { }

  public bool RequiresSession => this == DineIn;

  private sealed class DineInType()   : OrderType("DINE_IN",  1) { }
  private sealed class TakeawayType() : OrderType("TAKEAWAY", 2) { }
  private sealed class DeliveryType() : OrderType("DELIVERY", 3) { }
}
