using Ardalis.SmartEnum;

namespace Api.Core.Aggregates.OrderAggregate;

public class PaymentMethod : SmartEnum<PaymentMethod>
{
  public static readonly PaymentMethod Unknown      = new UnknownMethod();
  public static readonly PaymentMethod Cash         = new CashMethod();
  public static readonly PaymentMethod BankTransfer = new BankTransferMethod();

  private PaymentMethod(string name, int value) : base(name, value) { }

  private sealed class UnknownMethod()      : PaymentMethod("UNKNOWN",       0) { }
  private sealed class CashMethod()         : PaymentMethod("CASH",          1) { }
  private sealed class BankTransferMethod() : PaymentMethod("BANK_TRANSFER", 2) { }
}
