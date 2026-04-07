namespace Api.UseCases.Orders.Delete;

public record DeleteOrderCommand(int OrderId) : ICommand<Result>;
