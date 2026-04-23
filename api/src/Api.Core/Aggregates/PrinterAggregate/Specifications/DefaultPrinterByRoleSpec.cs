using Ardalis.Specification;

namespace Api.Core.Aggregates.PrinterAggregate.Specifications;

public class DefaultPrinterByRoleSpec : SingleResultSpecification<PrinterConfig>
{
  public DefaultPrinterByRoleSpec(PrinterRole role)
  {
    Query.Where(p => p.Role == role && p.IsDefault && p.IsActive && !p.IsDeleted);
  }
}
