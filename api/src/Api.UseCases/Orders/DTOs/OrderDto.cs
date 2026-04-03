namespace Api.UseCases.Orders.DTOs;

public record OrderItemDto(
  int ProductId,
  string ProductName,
  decimal UnitPrice,
  int Quantity,
  decimal Discount,
  decimal TotalPrice,
  string? Temperature,
  string? IceLevel,
  string? SugarLevel,
  bool IsTakeaway,
  bool IsFreeGift,
  string? Note
);

public record AppliedPromotionDto(
  int PromotionId,
  string PromoCode,
  decimal DiscountAmount
);

public record OrderDto(
  int Id,
  string OrderNumber,
  string Status,
  string PaymentStatus,
  string PaymentMethod,
  decimal? AmountReceived,
  decimal TipAmount,
  decimal TotalAmount,
  decimal TotalDiscount,
  decimal FinalAmount,
  DateTime OrderDate,
  Guid SessionId,
  string? TableCode,
  int? GuestCount,
  DateTime? CompletedAt,
  DateTime? PaidAt,
  List<OrderItemDto> Items,
  List<AppliedPromotionDto> Promotions,
  bool IsManual
);

public record PagedOrdersDto(
  List<OrderDto> Items,
  int TotalCount,
  int Page,
  int PageSize,
  decimal CashTotal,
  decimal BankTransferTotal,
  decimal TipTotal,
  int PendingCount,
  int ProcessingCount,
  int CompletedCount,
  int CancelledCount
);
