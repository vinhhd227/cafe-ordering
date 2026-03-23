using Api.UseCases.Zones.DTOs;

namespace Api.UseCases.Zones.Update;

public record UpdateZoneCommand(int ZoneId, string Name, int DisplayOrder) : ICommand<Result<ZoneDto>>;
