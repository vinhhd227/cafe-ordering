namespace Api.UseCases.Tables.DTOs;

public record TableDto(
  int Id,
  int Number,
  string Code,
  bool IsActive,
  string Status,
  Guid? ActiveSessionId
);
