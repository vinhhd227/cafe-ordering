using Ardalis.SmartEnum;

namespace Api.Core.Aggregates.PrinterAggregate;

public class PrintFormatterType : SmartEnum<PrintFormatterType>
{
  public static readonly PrintFormatterType EscPos   = new EscPosType();
  public static readonly PrintFormatterType Zpl      = new ZplType();
  public static readonly PrintFormatterType StarPrnt = new StarPrntType();

  private PrintFormatterType(string name, int value) : base(name, value) { }

  private sealed class EscPosType()   : PrintFormatterType("ESC_POS",   1) { }
  private sealed class ZplType()      : PrintFormatterType("ZPL",       2) { }
  private sealed class StarPrntType() : PrintFormatterType("STAR_PRNT", 3) { }
}
