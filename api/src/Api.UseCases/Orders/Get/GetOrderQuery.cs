using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.Get;

public record GetOrderQuery(int OrderId) : IQuery<Result<OrderDto>>;
