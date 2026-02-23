namespace Api.Core.Aggregates.CustomerAggregate.Specifications;

public class CustomerByIdSpec : Specification<Customer>
{
  public CustomerByIdSpec(string customerId)
  {
    Query.Where(c => c.Id == customerId && !c.IsDeleted);
  }
}
