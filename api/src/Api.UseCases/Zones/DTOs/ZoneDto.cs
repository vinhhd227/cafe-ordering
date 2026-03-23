namespace Api.UseCases.Zones.DTOs;

public record ZoneDto(int Id, string Name, int DisplayOrder, bool IsActive, int TableCount);
