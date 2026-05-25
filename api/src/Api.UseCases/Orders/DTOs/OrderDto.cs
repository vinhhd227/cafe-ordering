namespace Api.UseCases.Orders.DTOs;

public record OrderItemOptionDto(
  int OptionValueId,
  string GroupName,
  string Label,
  decimal PriceAdjustment);

public record OrderItemSelectedOptionDto(
  int OptionValueId,
  string GroupName,
  string ValueName,
  decimal UnitPrice,
  int Quantity,
  decimal Subtotal);

public record OrderItemDto(
  int Id,
  int ProductId,
  string ProductName,
  decimal UnitPrice,
  decimal OptionAdjustment,
  decimal OptionValueTotal,
  int Quantity,
  decimal Discount,
  decimal TotalPrice,
  List<OrderItemOptionDto> SelectedOptions,
  List<OrderItemSelectedOptionDto> SelectedOptionValues,
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
  Guid? SessionId,
  string? TableCode,
  int? GuestCount,
  DateTime? CompletedAt,
  DateTime? PaidAt,
  List<OrderItemDto> Items,
  List<AppliedPromotionDto> Promotions,
  bool IsManual,
  string OrderType       = "DINE_IN",
  string? CustomerName   = null,
  string? CustomerPhone  = null,
  string? DeliveryAddress = null,
  string? DeliveryNote   = null
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
