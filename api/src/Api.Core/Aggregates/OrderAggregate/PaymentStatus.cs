using Ardalis.SmartEnum;

namespace Api.Core.Aggregates.OrderAggregate;

public class PaymentStatus : SmartEnum<PaymentStatus>
{
  public static readonly PaymentStatus Unpaid   = new UnpaidStatus();
  public static readonly PaymentStatus Paid     = new PaidStatus();
  public static readonly PaymentStatus Refunded = new RefundedStatus();
  public static readonly PaymentStatus Voided   = new VoidedStatus();

  private PaymentStatus(string name, int value) : base(name, value) { }

  private sealed class UnpaidStatus()   : PaymentStatus("UNPAID",   1) { }
  private sealed class PaidStatus()     : PaymentStatus("PAID",     2) { }
  private sealed class RefundedStatus() : PaymentStatus("REFUNDED", 3) { }
  private sealed class VoidedStatus()   : PaymentStatus("VOIDED",   4) { }
}
