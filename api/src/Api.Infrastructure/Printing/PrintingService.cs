using System.Text.Json;
using Api.Core.Aggregates.PrinterAggregate;
using Api.Infrastructure.Printing.Abstractions;
using Api.UseCases.Printing.DTOs;
using Api.UseCases.Printing.Interfaces;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Printing;

public class PrintingService(
  IEnumerable<IPrintFormatter>  formatters,
  IEnumerable<IPrinterTransport> transports,
  ILogger<PrintingService>      logger)
  : IPrintingService
{
  public async Task<PrintLabelsResultDto> PrintDrinkLabelsAsync(
    PrinterConfig              config,
    string                     orderNumber,
    string?                    tableCode,
    IReadOnlyList<DrinkLabelItemInfo> items,
    int                        copiesPerItem,
    CancellationToken          ct = default)
  {
    var formatter = formatters.FirstOrDefault(f => f.Supports(config.FormatterType));
    if (formatter is null)
      return new PrintLabelsResultDto(false, false, null, $"No formatter for {config.FormatterType.Name}.");

    var transport = transports.FirstOrDefault(t => t.Supports(config.TransportType));
    if (transport is null)
      return new PrintLabelsResultDto(false, false, null, $"No transport for {config.TransportType.Name}.");

    bool isWebUsb = config.TransportType == PrintTransportType.WebUsb;
    var allBytes = new List<byte[]>();

    var now = DateTime.Now;
    int totalItems = items.Count;

    for (int i = 0; i < totalItems; i++)
    {
      var item = items[i];
      var data = new DrinkLabelData(
        orderNumber, tableCode, item.ProductName, item.Quantity,
        item.Temperature, item.IceLevel, item.SugarLevel,
        item.Note, item.IsTakeaway,
        ItemIndex: i + 1, TotalItems: totalItems,
        PrintedAt: now);

      for (int copy = 0; copy < copiesPerItem; copy++)
      {
        var bytes = formatter.FormatDrinkLabel(data, config);

        if (isWebUsb)
        {
          allBytes.Add(bytes);
        }
        else
        {
          try
          {
            await transport.SendAsync(bytes, config.ConnectionParams, ct);
          }
          catch (Exception ex)
          {
            logger.LogError(ex, "Failed to send label to printer {PrinterId}", config.Id);
            return new PrintLabelsResultDto(false, false, null, ex.Message);
          }
        }
      }
    }

    if (isWebUsb)
    {
      var combined = CombineByteArrays(allBytes);
      return new PrintLabelsResultDto(false, true, Convert.ToBase64String(combined), null);
    }

    return new PrintLabelsResultDto(true, false, null, null);
  }

  public async Task<bool> TestConnectionAsync(PrinterConfig config, CancellationToken ct = default)
  {
    var formatter = formatters.FirstOrDefault(f => f.Supports(config.FormatterType));
    if (formatter is null) return false;

    var transport = transports.FirstOrDefault(t => t.Supports(config.TransportType));
    if (transport is null) return false;

    if (config.TransportType == PrintTransportType.WebUsb)
      return true;

    try
    {
      return await transport.TestConnectionAsync(config.ConnectionParams, ct);
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Test connection failed for printer {PrinterId}", config.Id);
      return false;
    }
  }

  private static byte[] CombineByteArrays(List<byte[]> arrays)
  {
    var total = arrays.Sum(a => a.Length);
    var result = new byte[total];
    int offset = 0;
    foreach (var arr in arrays)
    {
      Buffer.BlockCopy(arr, 0, result, offset, arr.Length);
      offset += arr.Length;
    }
    return result;
  }
}
