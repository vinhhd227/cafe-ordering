using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.List;

public record ListOrdersQuery(
  string? Status = null,
  DateTime? DateFrom = null,
  DateTime? DateTo = null,
  int Page = 1,
  int PageSize = 20
) : IQuery<Result<PagedOrdersDto>>;
