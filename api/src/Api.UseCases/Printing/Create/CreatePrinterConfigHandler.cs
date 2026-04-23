using Api.Core.Aggregates.PrinterAggregate;
using Api.UseCases.Printing.DTOs;

namespace Api.UseCases.Printing.Create;

public class CreatePrinterConfigHandler(IRepositoryBase<PrinterConfig> repo)
  : ICommandHandler<CreatePrinterConfigCommand, Result<PrinterConfigDto>>
{
  public async ValueTask<Result<PrinterConfigDto>> Handle(CreatePrinterConfigCommand cmd, CancellationToken ct)
  {
    if (!PrinterRole.TryFromName(cmd.Role, true, out var role))
      return Result.Invalid(new ValidationError("Role", $"Invalid role '{cmd.Role}'."));

    if (!PrintFormatterType.TryFromName(cmd.FormatterType, true, out var formatter))
      return Result.Invalid(new ValidationError("FormatterType", $"Invalid formatter '{cmd.FormatterType}'."));

    if (!PrintTransportType.TryFromName(cmd.TransportType, true, out var transport))
      return Result.Invalid(new ValidationError("TransportType", $"Invalid transport '{cmd.TransportType}'."));

    var config = PrinterConfig.Create(cmd.Name, role, formatter, transport, cmd.ConnectionParams, cmd.PaperWidthMm);
    await repo.AddAsync(config, ct);

    return Result.Success(ToDto(config));
  }

  private static PrinterConfigDto ToDto(PrinterConfig c) => new(
    c.Id, c.Name,
    c.Role.Name, c.FormatterType.Name, c.TransportType.Name,
    c.ConnectionParams, c.PaperWidthMm, c.IsDefault, c.IsActive);
}
