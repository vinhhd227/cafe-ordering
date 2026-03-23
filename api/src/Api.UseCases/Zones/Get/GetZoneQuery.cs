using Api.UseCases.Zones.DTOs;

namespace Api.UseCases.Zones.Get;

public record GetZoneQuery(int ZoneId) : IQuery<Result<ZoneDto>>;
