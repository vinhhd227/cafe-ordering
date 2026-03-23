using Api.UseCases.Zones.DTOs;

namespace Api.UseCases.Zones.Create;

public record CreateZoneCommand(string Name, int DisplayOrder) : ICommand<Result<ZoneDto>>;
