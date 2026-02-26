using Api.UseCases.Common.Interfaces;

namespace Api.UseCases.Auth.CreateRole;

public record CreateRoleCommand(string Name, string? Description) : ICommand<Result>;
