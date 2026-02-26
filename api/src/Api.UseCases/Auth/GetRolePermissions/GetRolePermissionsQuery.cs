using Api.UseCases.Common.Interfaces;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.GetRolePermissions;

public record GetRolePermissionsQuery(Guid RoleId) : IQuery<Result<List<RolePermissionDto>>>;
