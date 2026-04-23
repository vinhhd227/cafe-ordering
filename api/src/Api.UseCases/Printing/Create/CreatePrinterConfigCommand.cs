using Api.UseCases.Printing.DTOs;

namespace Api.UseCases.Printing.Create;

public record CreatePrinterConfigCommand(
  string Name,
  string Role,
  string FormatterType,
  string TransportType,
  string ConnectionParams,
  int    PaperWidthMm
) : ICommand<Result<PrinterConfigDto>>;
