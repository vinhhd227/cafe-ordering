namespace Api.UseCases.Orders.UpdateStatus;

public record UpdateOrderStatusCommand(int OrderId, string Status) : ICommand<Result>;
