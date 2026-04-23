using Api.Core.Aggregates.PrinterAggregate;
using Api.Core.Aggregates.PrinterAggregate.Specifications;

namespace Api.UseCases.Printing.SetDefault;

public class SetDefaultPrinterHandler(IRepositoryBase<PrinterConfig> repo)
  : ICommandHandler<SetDefaultPrinterCommand, Result>
{
  public async ValueTask<Result> Handle(SetDefaultPrinterCommand cmd, CancellationToken ct)
  {
    var config = await repo.FirstOrDefaultAsync(new PrinterConfigByIdSpec(cmd.Id), ct);
    if (config is null)
      return Result.NotFound($"Printer {cmd.Id} not found.");

    // Unset current default for this role
    var currentDefault = await repo.FirstOrDefaultAsync(new DefaultPrinterByRoleSpec(config.Role), ct);
    if (currentDefault is not null && currentDefault.Id != cmd.Id)
    {
      currentDefault.UnsetDefault();
      await repo.UpdateAsync(currentDefault, ct);
    }

    config.SetAsDefault();
    await repo.UpdateAsync(config, ct);

    return Result.Success();
  }
}
