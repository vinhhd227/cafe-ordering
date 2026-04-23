using Ardalis.SmartEnum;

namespace Api.Core.Aggregates.PrinterAggregate;

public class PrintTransportType : SmartEnum<PrintTransportType>
{
  public static readonly PrintTransportType UsbDevice = new UsbDeviceType();
  public static readonly PrintTransportType Tcp       = new TcpType();
  public static readonly PrintTransportType WebUsb    = new WebUsbType();

  private PrintTransportType(string name, int value) : base(name, value) { }

  private sealed class UsbDeviceType() : PrintTransportType("USB_DEVICE", 1) { }
  private sealed class TcpType()       : PrintTransportType("TCP",        2) { }
  private sealed class WebUsbType()    : PrintTransportType("WEB_USB",    3) { }
}
