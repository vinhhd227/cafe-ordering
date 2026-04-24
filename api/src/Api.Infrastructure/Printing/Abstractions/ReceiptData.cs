namespace Api.Infrastructure.Printing.Abstractions;

public record ReceiptData(
  string                      OrderNumber,
  string?                     TableCode,
  IReadOnlyList<ReceiptItem>  Items,
  decimal                     Subtotal,
  decimal                     Discount,
  decimal                     Total,
  string?                     PaymentMethod,
  DateTime                    PrintedAt,
  string?                     CafeName = null,
  string?                     CafeAddress = null,
  string?                     CafePhone = null,
  string?                     QrUrl = null
);

public record ReceiptItem(
  string  ProductName,
  int     Quantity,
  decimal UnitPrice,
  decimal TotalPrice
);
