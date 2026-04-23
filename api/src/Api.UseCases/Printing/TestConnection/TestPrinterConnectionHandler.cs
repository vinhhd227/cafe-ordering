using Api.Core.Aggregates.PrinterAggregate;
using Api.Core.Aggregates.PrinterAggregate.Specifications;
using Api.UseCases.Printing.Interfaces;

namespace Api.UseCases.Printing.TestConnection;

public class TestPrinterConnectionHandler(
  IReadRepositoryBase<PrinterConfig> repo,
  IPrintingService                   printingService)
  : ICommandHandler<TestPrinterConnectionCommand, Result<bool>>
{
  public async ValueTask<Result<bool>> Handle(TestPrinterConnectionCommand cmd, CancellationToken ct)
  {
    var config = await repo.FirstOrDefaultAsync(new PrinterConfigByIdSpec(cmd.PrinterId), ct);
    if (config is null)
      return Result.NotFound($"Printer {cmd.PrinterId} not found.");

    var ok = await printingService.TestConnectionAsync(config, ct);
    return Result.Success(ok);
  }
}
