using Api.Core.Aggregates.PrinterAggregate;
using Api.Core.Aggregates.PrinterAggregate.Specifications;
using Api.UseCases.Printing.DTOs;

namespace Api.UseCases.Printing.List;

public class ListPrinterConfigsHandler(IReadRepositoryBase<PrinterConfig> repo)
  : IQueryHandler<ListPrinterConfigsQuery, Result<List<PrinterConfigDto>>>
{
  public async ValueTask<Result<List<PrinterConfigDto>>> Handle(ListPrinterConfigsQuery request, CancellationToken ct)
  {
    var configs = await repo.ListAsync(new AllPrinterConfigsSpec(), ct);
    var dtos = configs
      .OrderBy(p => p.Role.Name)
      .ThenByDescending(p => p.IsDefault)
      .Select(ToDto).ToList();
    return Result.Success(dtos);
  }

  private static PrinterConfigDto ToDto(PrinterConfig c) => new(
    c.Id, c.Name,
    c.Role.Name, c.FormatterType.Name, c.TransportType.Name,
    c.ConnectionParams, c.PaperWidthMm, c.IsDefault, c.IsActive);
}
