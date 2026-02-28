using Api.Core.Aggregates.GuestSessionAggregate;

namespace Api.UseCases.Sessions.DTOs;

public record SessionContextDto(
  Guid SessionId,
  int? TableId,
  GuestSessionStatus Status
);
