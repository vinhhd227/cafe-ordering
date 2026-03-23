using Api.UseCases.Zones.DTOs;

namespace Api.UseCases.Zones.List;

public record ListZonesQuery : IQuery<Result<List<ZoneDto>>>;
