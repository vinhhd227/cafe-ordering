namespace Api.UseCases.Zones.Delete;

public record DeleteZoneCommand(int ZoneId, string DeletedBy) : ICommand<Result>;
