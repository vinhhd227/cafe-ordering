using Api.Core.Aggregates.PrinterAggregate;

namespace Api.Infrastructure.Printing.Abstractions;

public interface IPrinterTransport
{
  bool Supports(PrintTransportType type);
  Task SendAsync(byte[] data, string connectionParamsJson, CancellationToken ct = default);
  Task<bool> TestConnectionAsync(string connectionParamsJson, CancellationToken ct = default);
}
