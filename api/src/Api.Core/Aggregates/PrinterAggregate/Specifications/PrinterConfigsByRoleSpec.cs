using Ardalis.Specification;

namespace Api.Core.Aggregates.PrinterAggregate.Specifications;

public class PrinterConfigsByRoleSpec : Specification<PrinterConfig>
{
  public PrinterConfigsByRoleSpec(PrinterRole role)
  {
    Query
      .Where(p => p.Role == role && !p.IsDeleted)
      .OrderByDescending(p => p.IsDefault);
  }
}
