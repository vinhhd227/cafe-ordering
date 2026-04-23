using Api.Core.Aggregates.PrinterAggregate;

namespace Api.Infrastructure.Printing.Abstractions;

public interface IPrintFormatter
{
  bool Supports(PrintFormatterType type);
  byte[] FormatDrinkLabel(DrinkLabelData data, PrinterConfig config);
  byte[] FormatTestPage(PrinterConfig config);
}
