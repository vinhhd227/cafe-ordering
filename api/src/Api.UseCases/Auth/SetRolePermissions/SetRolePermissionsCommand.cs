using Api.UseCases.Common.Interfaces;

namespace Api.UseCases.Auth.SetRolePermissions;

public record SetRolePermissionsCommand(Guid RoleId, IList<string> Permissions) : ICommand<Result>;
