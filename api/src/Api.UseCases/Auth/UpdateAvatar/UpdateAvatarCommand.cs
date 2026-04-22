namespace Api.UseCases.Auth.UpdateAvatar;

public record UpdateAvatarCommand(Guid UserId, string AvatarUrl) : ICommand<Result<string>>;
