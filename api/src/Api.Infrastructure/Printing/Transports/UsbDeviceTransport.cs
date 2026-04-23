using System.Runtime.InteropServices;
using System.Text.Json;
using Api.Core.Aggregates.PrinterAggregate;
using Api.Infrastructure.Printing.Abstractions;

namespace Api.Infrastructure.Printing.Transports;

public class UsbDeviceTransport(ILogger<UsbDeviceTransport> logger) : IPrinterTransport
{
  public bool Supports(PrintTransportType type) => type == PrintTransportType.UsbDevice;

  public async Task SendAsync(byte[] data, string connectionParamsJson, CancellationToken ct = default)
  {
    var p    = Parse(connectionParamsJson);
    var path = p.DevicePath ?? throw new InvalidOperationException("DevicePath is required (e.g. /dev/usb/lp0 on Linux, \\\\.\\USB001 on Windows).");
    logger.LogInformation("Sending {Bytes} bytes to USB device '{Path}'", data.Length, path);

    await using var fs = new FileStream(path, FileMode.Open, FileAccess.Write,
      FileShare.ReadWrite, bufferSize: 4096, useAsync: true);
    await fs.WriteAsync(data, ct);
    await fs.FlushAsync(ct);
  }

  public async Task<bool> TestConnectionAsync(string connectionParamsJson, CancellationToken ct = default)
  {
    var p          = Parse(connectionParamsJson);
    var devicePath = p.DevicePath;
    if (string.IsNullOrWhiteSpace(devicePath)) return false;

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && devicePath.StartsWith(@"\\.\"))
    {
      try
      {
        await using var fs = new FileStream(devicePath, FileMode.Open, FileAccess.Write,
          FileShare.None, bufferSize: 1, useAsync: true);
        return true;
      }
      catch { return false; }
    }

    return File.Exists(devicePath);
  }

  private static UsbDeviceParams Parse(string json)
  {
    var result = JsonSerializer.Deserialize<UsbDeviceParams>(json,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    return result ?? throw new InvalidOperationException("Invalid USB device connection params.");
  }

  // DevicePath: Linux → /dev/usb/lp0  |  Windows → \\.\USB001
  private record UsbDeviceParams(string? DevicePath = null);
}
