using Api.UseCases.Printing.DTOs;

namespace Api.UseCases.Printing.Update;

public record UpdatePrinterConfigCommand(
  int    Id,
  string Name,
  string FormatterType,
  string TransportType,
  string ConnectionParams,
  int    PaperWidthMm
) : ICommand<Result<PrinterConfigDto>>;
