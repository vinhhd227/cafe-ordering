using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.List;

public record ListOrdersQuery(string? Status = null) : IQuery<Result<List<OrderDto>>>;
