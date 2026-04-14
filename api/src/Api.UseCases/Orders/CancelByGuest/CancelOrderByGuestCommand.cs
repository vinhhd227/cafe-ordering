namespace Api.UseCases.Orders.CancelByGuest;

public record CancelOrderByGuestCommand(int OrderId, Guid SessionId)
  : ICommand<Result>;
