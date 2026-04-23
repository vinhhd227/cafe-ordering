using Api.Infrastructure.Printing.Abstractions;
using Api.Infrastructure.Printing.Formatters;
using Api.Infrastructure.Printing.Transports;
using Api.UseCases.Printing.Interfaces;

namespace Api.Infrastructure.Printing;

public static class PrintingServiceExtensions
{
  public static IServiceCollection AddPrinting(this IServiceCollection services)
  {
    services.AddScoped<IPrintFormatter, EscPosPrintFormatter>();
    services.AddScoped<IPrinterTransport, UsbDeviceTransport>();
    services.AddScoped<IPrintingService, PrintingService>();
    return services;
  }
}
