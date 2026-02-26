using Api.UseCases.Common.Interfaces;

namespace Api.UseCases.Auth.DeleteRole;

public record DeleteRoleCommand(Guid RoleId) : ICommand<Result>;
