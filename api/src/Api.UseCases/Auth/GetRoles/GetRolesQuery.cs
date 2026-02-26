using Api.UseCases.Common.Interfaces;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.GetRoles;

public record GetRolesQuery(
  int Page,
  int PageSize,
  string? Search) : IQuery<Result<PagedRolesDto>>;
