namespace Api.Core.Aggregates.ExpenseAggregate.Specifications;

public class ExpenseByIdSpec : Specification<Expense>
{
  public ExpenseByIdSpec(int id)
  {
    Query.Where(e => e.Id == id && !e.IsDeleted);
  }
}
