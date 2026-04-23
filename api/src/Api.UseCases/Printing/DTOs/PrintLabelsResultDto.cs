namespace Api.UseCases.Printing.DTOs;

public record PrintLabelsResultDto(
  bool    Success,
  bool    RequiresClientPrint,
  string? Bytes,
  string? Error
);
