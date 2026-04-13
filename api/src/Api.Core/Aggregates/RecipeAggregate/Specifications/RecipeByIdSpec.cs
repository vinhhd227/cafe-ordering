namespace Api.Core.Aggregates.RecipeAggregate.Specifications;

public class RecipeByIdSpec : Specification<Recipe>
{
  public RecipeByIdSpec(int id)
  {
    Query.Where(r => r.Id == id && !r.IsDeleted);
  }
}
