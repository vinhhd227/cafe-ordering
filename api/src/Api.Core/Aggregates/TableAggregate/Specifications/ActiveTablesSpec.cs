using Api.Core.Aggregates.ZoneAggregate;

namespace Api.Core.Aggregates.TableAggregate.Specifications;

public class ActiveTablesSpec : Specification<Table>
{
  public ActiveTablesSpec()
  {
    Query
      .Where(t => !t.IsDeleted && t.IsActive)
      .Include(t => t.Zone)
      .OrderBy(t => t.Code);
  }
}
