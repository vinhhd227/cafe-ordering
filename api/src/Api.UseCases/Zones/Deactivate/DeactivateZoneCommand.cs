namespace Api.UseCases.Zones.Deactivate;

public record DeactivateZoneCommand(int ZoneId) : ICommand<Result>;
