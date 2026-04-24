using Api.Core.Aggregates.PrinterAggregate;
using Api.UseCases.Printing.DTOs;

namespace Api.UseCases.Printing.Interfaces;

public interface IPrintingService
{
  Task<PrintLabelsResultDto> PrintDrinkLabelsAsync(
    PrinterConfig              config,
    string                     orderNumber,
    string?                    tableCode,
    IReadOnlyList<DrinkLabelItemInfo> items,
    int                        copiesPerItem,
    CancellationToken          ct = default);

  Task<PrintLabelsResultDto> PrintBillAsync(
    PrinterConfig             config,
    string                    orderNumber,
    string?                   tableCode,
    IReadOnlyList<BillItemInfo> items,
    decimal                   subtotal,
    decimal                   discount,
    decimal                   total,
    string?                   paymentMethod,
    string?                   cafeName,
    string?                   cafeAddress,
    string?                   cafePhone,
    string?                   qrUrl,
    CancellationToken         ct = default);

  Task<bool> TestConnectionAsync(PrinterConfig config, CancellationToken ct = default);
}

public record BillItemInfo(
  string  ProductName,
  int     Quantity,
  decimal UnitPrice,
  decimal TotalPrice
);

public record DrinkLabelItemInfo(
  int     ItemId,
  string  ProductName,
  int     Quantity,
  string? Temperature,
  string? IceLevel,
  string? SugarLevel,
  string? Note,
  bool    IsTakeaway
);
