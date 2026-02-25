namespace Api.UseCases.Auth.ChangeUserRole;

public record ChangeUserRoleCommand(Guid UserId, string Role) : ICommand<Result>;
