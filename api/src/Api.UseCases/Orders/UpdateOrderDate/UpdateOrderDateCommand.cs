namespace Api.UseCases.Orders.UpdateOrderDate;

public record UpdateOrderDateCommand(int OrderId, DateTime OrderDate) : ICommand<Result>;
