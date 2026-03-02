namespace Api.UseCases.Tables.DTOs;

public record TableDto(
  int Id,
  string Code,
  bool IsActive,
  string Status,
  Guid? ActiveSessionId
);
