using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.GetUsers;

public record GetUsersQuery(
  int Page,
  int PageSize,
  string? Search,
  string? Role,
  bool? IsActive) : IQuery<Result<PagedUsersDto>>;
