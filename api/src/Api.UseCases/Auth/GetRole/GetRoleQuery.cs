using Api.UseCases.Common.Interfaces;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.GetRole;

public record GetRoleQuery(Guid RoleId) : IQuery<Result<RoleDto>>;
