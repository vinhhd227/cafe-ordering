namespace Api.UseCases.Printing.DTOs;

public record PrinterConfigDto(
  int    Id,
  string Name,
  string Role,
  string FormatterType,
  string TransportType,
  string ConnectionParams,
  int    PaperWidthMm,
  bool   IsDefault,
  bool   IsActive
);
