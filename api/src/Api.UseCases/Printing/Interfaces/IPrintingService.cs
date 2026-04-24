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
    string?                   qrRaw,
    string?                   bankAccountName,
    string?                   bankAccountNumber,
    string?                   bankBranch,
    string?                   wifiName,
    string?                   wifiPassword,
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
  decimal UnitPrice,
  string? Temperature,
  string? IceLevel,
  string? SugarLevel,
  string? Note,
  bool    IsTakeaway
);
