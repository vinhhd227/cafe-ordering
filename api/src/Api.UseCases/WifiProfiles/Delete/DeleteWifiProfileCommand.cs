namespace Api.UseCases.WifiProfiles.Delete;

public record DeleteWifiProfileCommand(int Id, string DeletedBy) : ICommand<Result>;
