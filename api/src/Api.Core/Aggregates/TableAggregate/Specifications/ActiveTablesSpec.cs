namespace Api.Core.Aggregates.TableAggregate.Specifications;

public class ActiveTablesSpec : Specification<Table>
{
  public ActiveTablesSpec()
  {
    Query
      .Where(t => !t.IsDeleted && t.IsActive)
      .OrderBy(t => t.Code);
  }
}
