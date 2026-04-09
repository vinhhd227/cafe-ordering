using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.DailyOrders;

public record GetDailyOrdersQuery(DateOnly Date) : IQuery<Result<DailyOrdersResponseDto>>;
