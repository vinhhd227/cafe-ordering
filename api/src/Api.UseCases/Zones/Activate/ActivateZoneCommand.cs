namespace Api.UseCases.Zones.Activate;

public record ActivateZoneCommand(int ZoneId) : ICommand<Result>;
