using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Promotions.Remove;

public record RemovePromotionCommand(int OrderId, int PromotionId, Guid? SessionId) : ICommand<Result<OrderDto>>;
