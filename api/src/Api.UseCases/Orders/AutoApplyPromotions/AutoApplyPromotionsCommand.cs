using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.AutoApplyPromotions;

public record AutoApplyPromotionsCommand(int OrderId) : ICommand<Result<OrderDto>>;
