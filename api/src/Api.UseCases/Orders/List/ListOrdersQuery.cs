using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.List;

public record ListOrdersQuery(
  string? Status = null,
  string? PaymentStatus = null,
  string? OrderNumber = null,
  decimal? MinAmount = null,
  decimal? MaxAmount = null,
  string? TableCode = null,
  DateTime? DateFrom = null,
  DateTime? DateTo = null,
  int Page = 1,
  int PageSize = 20
) : IQuery<Result<PagedOrdersDto>>;
