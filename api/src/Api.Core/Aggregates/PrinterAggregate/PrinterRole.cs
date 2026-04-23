using Ardalis.SmartEnum;

namespace Api.Core.Aggregates.PrinterAggregate;

public class PrinterRole : SmartEnum<PrinterRole>
{
  public static readonly PrinterRole DrinkLabel = new DrinkLabelRole();
  public static readonly PrinterRole Receipt    = new ReceiptRole();
  public static readonly PrinterRole Kitchen    = new KitchenRole();

  private PrinterRole(string name, int value) : base(name, value) { }

  private sealed class DrinkLabelRole() : PrinterRole("DRINK_LABEL", 1) { }
  private sealed class ReceiptRole()    : PrinterRole("RECEIPT",     2) { }
  private sealed class KitchenRole()    : PrinterRole("KITCHEN",     3) { }
}
