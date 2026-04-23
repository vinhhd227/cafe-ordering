using Api.Core.Aggregates.PrinterAggregate;
using Api.Core.Aggregates.PrinterAggregate.Specifications;

namespace Api.UseCases.Printing.Delete;

public class DeletePrinterConfigHandler(IRepositoryBase<PrinterConfig> repo)
  : ICommandHandler<DeletePrinterConfigCommand, Result>
{
  public async ValueTask<Result> Handle(DeletePrinterConfigCommand cmd, CancellationToken ct)
  {
    var config = await repo.FirstOrDefaultAsync(new PrinterConfigByIdSpec(cmd.Id), ct);
    if (config is null)
      return Result.NotFound($"Printer {cmd.Id} not found.");

    config.Delete(cmd.DeletedBy);
    await repo.UpdateAsync(config, ct);

    return Result.Success();
  }
}
