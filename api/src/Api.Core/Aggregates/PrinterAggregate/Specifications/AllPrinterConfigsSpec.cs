using Ardalis.Specification;

namespace Api.Core.Aggregates.PrinterAggregate.Specifications;

public class AllPrinterConfigsSpec : Specification<PrinterConfig>
{
  public AllPrinterConfigsSpec()
  {
    Query.Where(p => !p.IsDeleted);
  }
}
