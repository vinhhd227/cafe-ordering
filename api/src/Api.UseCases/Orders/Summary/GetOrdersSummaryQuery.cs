using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.Summary;

public record GetOrdersSummaryQuery(DateTime? DateFrom, DateTime? DateTo)
  : IQuery<Result<OrdersSummaryDto>>;
