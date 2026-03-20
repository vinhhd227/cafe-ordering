namespace Api.UseCases.Tables.DTOs;

public record PublicTableDto(
  int Id,
  string Code,
  string Status  // Available | Occupied
);
