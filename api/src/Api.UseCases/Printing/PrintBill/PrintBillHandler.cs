using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.PrinterAggregate;
using Api.Core.Aggregates.PrinterAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Printing.DTOs;
using Api.UseCases.Printing.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Api.UseCases.Printing.PrintBill;

public class PrintBillHandler(
  IReadRepositoryBase<Order>         orderRepo,
  IReadRepositoryBase<GuestSession>  sessionRepo,
  IReadRepositoryBase<Table>         tableRepo,
  IReadRepositoryBase<PrinterConfig> printerRepo,
  IPrintingService                   printingService,
  IConfiguration                     configuration)
  : ICommandHandler<PrintBillCommand, Result<PrintLabelsResultDto>>
{
  public async ValueTask<Result<PrintLabelsResultDto>> Handle(PrintBillCommand cmd, CancellationToken ct)
  {
    var order = await orderRepo.FirstOrDefaultAsync(new OrderByIdWithItemsSpec(cmd.OrderId), ct);
    if (order is null)
      return Result.NotFound($"Order {cmd.OrderId} not found.");

    // Resolve table code via session
    string? tableCode = null;
    var session = await sessionRepo.FirstOrDefaultAsync(new SessionByIdSpec(order.SessionId), ct);
    if (session?.TableId is { } tableId)
    {
      var table = await tableRepo.FirstOrDefaultAsync(new TableByIdSpec(tableId), ct);
      tableCode = table?.Code;
    }

    // Resolve printer
    PrinterConfig? printerConfig;
    if (cmd.PrinterId.HasValue)
    {
      printerConfig = await printerRepo.FirstOrDefaultAsync(new PrinterConfigByIdSpec(cmd.PrinterId.Value), ct);
      if (printerConfig is null)
        return Result.NotFound($"Printer {cmd.PrinterId} not found.");
    }
    else
    {
      printerConfig = await printerRepo.FirstOrDefaultAsync(new DefaultPrinterByRoleSpec(PrinterRole.Receipt), ct);
      if (printerConfig is null)
        return Result.Error("No default printer configured for role 'RECEIPT'.");
    }

    var items = order.Items
      .Select(i => new BillItemInfo(i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice))
      .ToList();

    var cafeName    = configuration["CafeInfo:Name"];
    var cafeAddress = configuration["CafeInfo:Address"];
    var cafePhone   = configuration["CafeInfo:Phone"];
    var qrUrl       = configuration["CafeInfo:QrUrl"];

    var result = await printingService.PrintBillAsync(
      printerConfig,
      order.OrderNumber,
      tableCode,
      items,
      order.TotalAmount,
      order.TotalDiscount,
      order.FinalAmount,
      order.PaymentMethod?.Name,
      cafeName,
      cafeAddress,
      cafePhone,
      qrUrl,
      ct);

    return Result.Success(result);
  }
}
