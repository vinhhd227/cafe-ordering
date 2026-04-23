using Api.Core.Aggregates.PrinterAggregate;
using Api.Core.Aggregates.PrinterAggregate.Specifications;
using Api.UseCases.Printing.DTOs;

namespace Api.UseCases.Printing.Update;

public class UpdatePrinterConfigHandler(IRepositoryBase<PrinterConfig> repo)
  : ICommandHandler<UpdatePrinterConfigCommand, Result<PrinterConfigDto>>
{
  public async ValueTask<Result<PrinterConfigDto>> Handle(UpdatePrinterConfigCommand cmd, CancellationToken ct)
  {
    var config = await repo.FirstOrDefaultAsync(new PrinterConfigByIdSpec(cmd.Id), ct);
    if (config is null)
      return Result.NotFound($"Printer {cmd.Id} not found.");

    if (!PrintFormatterType.TryFromName(cmd.FormatterType, true, out var formatter))
      return Result.Invalid(new ValidationError("FormatterType", $"Invalid formatter '{cmd.FormatterType}'."));

    if (!PrintTransportType.TryFromName(cmd.TransportType, true, out var transport))
      return Result.Invalid(new ValidationError("TransportType", $"Invalid transport '{cmd.TransportType}'."));

    config.Update(cmd.Name, formatter, transport, cmd.ConnectionParams, cmd.PaperWidthMm);
    await repo.UpdateAsync(config, ct);

    return Result.Success(new PrinterConfigDto(
      config.Id, config.Name,
      config.Role.Name, config.FormatterType.Name, config.TransportType.Name,
      config.ConnectionParams, config.PaperWidthMm, config.IsDefault, config.IsActive));
  }
}
