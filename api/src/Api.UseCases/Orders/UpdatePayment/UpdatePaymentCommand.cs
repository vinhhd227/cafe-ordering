namespace Api.UseCases.Orders.UpdatePayment;

public record UpdatePaymentCommand(int OrderId, string PaymentStatus, string PaymentMethod,
  decimal? AmountReceived, decimal TipAmount) : ICommand<Result>;
