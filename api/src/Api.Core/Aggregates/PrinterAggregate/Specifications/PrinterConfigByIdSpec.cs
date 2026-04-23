using Ardalis.Specification;

namespace Api.Core.Aggregates.PrinterAggregate.Specifications;

public class PrinterConfigByIdSpec : SingleResultSpecification<PrinterConfig>
{
  public PrinterConfigByIdSpec(int id)
  {
    Query.Where(p => p.Id == id && !p.IsDeleted);
  }
}
