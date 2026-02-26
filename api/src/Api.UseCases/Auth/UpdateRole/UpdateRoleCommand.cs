using Api.UseCases.Common.Interfaces;

namespace Api.UseCases.Auth.UpdateRole;

public record UpdateRoleCommand(Guid RoleId, string Name, string? Description) : ICommand<Result>;
