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

namespace Api.UseCases.Printing.PrintLabels;

public class PrintDrinkLabelsHandler(
  IReadRepositoryBase<Order>        orderRepo,
  IReadRepositoryBase<GuestSession> sessionRepo,
  IReadRepositoryBase<Table>        tableRepo,
  IReadRepositoryBase<PrinterConfig> printerRepo,
  IPrintingService                  printingService)
  : ICommandHandler<PrintDrinkLabelsCommand, Result<PrintLabelsResultDto>>
{
  public async ValueTask<Result<PrintLabelsResultDto>> Handle(PrintDrinkLabelsCommand cmd, CancellationToken ct)
  {
    // Load order with items
    var order = await orderRepo.FirstOrDefaultAsync(new OrderByIdWithItemsSpec(cmd.OrderId), ct);
    if (order is null)
      return Result.NotFound($"Order {cmd.OrderId} not found.");

    var items = order.Items.AsEnumerable();
    if (cmd.ItemIds is { Length: > 0 })
      items = items.Where(i => cmd.ItemIds.Contains(i.Id));

    var itemList = items.ToList();
    if (itemList.Count == 0)
      return Result.Invalid(new ValidationError("ItemIds", "No items to print."));

    // Load table code via session
    string? tableCode = null;
    var session = await sessionRepo.FirstOrDefaultAsync(new SessionByIdSpec(order.SessionId), ct);
    if (session?.TableId is { } tableId)
    {
      var table = await tableRepo.FirstOrDefaultAsync(new TableByIdSpec(tableId), ct);
      tableCode = table?.Code;
    }

    // Resolve printer
    PrinterConfig? printerConfig = null;
    if (cmd.PrinterId.HasValue)
    {
      printerConfig = await printerRepo.FirstOrDefaultAsync(new PrinterConfigByIdSpec(cmd.PrinterId.Value), ct);
      if (printerConfig is null)
        return Result.NotFound($"Printer {cmd.PrinterId} not found.");
    }
    else
    {
      var role = PrinterRole.DrinkLabel;
      if (cmd.Role is not null && !PrinterRole.TryFromName(cmd.Role, true, out role))
        return Result.Invalid(new ValidationError("Role", $"Invalid role '{cmd.Role}'."));

      printerConfig = await printerRepo.FirstOrDefaultAsync(new DefaultPrinterByRoleSpec(role), ct);
      if (printerConfig is null)
        return Result.Error($"No default printer configured for role '{role.Name}'.");
    }

    var drinkItems = itemList.Select(i => new DrinkLabelItemInfo(
      i.Id,
      i.ProductName,
      i.Quantity,
      i.Temperature?.Name,
      i.IceLevel?.Name,
      i.SugarLevel?.Name,
      i.Note,
      i.IsTakeaway)).ToList();

    var result = await printingService.PrintDrinkLabelsAsync(
      printerConfig, order.OrderNumber, tableCode, drinkItems, cmd.CopiesPerItem, ct);

    return Result.Success(result);
  }
}
