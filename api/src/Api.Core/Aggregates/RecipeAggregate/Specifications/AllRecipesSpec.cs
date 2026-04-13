namespace Api.Core.Aggregates.RecipeAggregate.Specifications;

public class AllRecipesSpec : Specification<Recipe>
{
  public AllRecipesSpec(string? search = null, RecipeType? type = null, RecipeCategory? category = null)
  {
    Query.Where(r => !r.IsDeleted);

    if (!string.IsNullOrWhiteSpace(search))
      Query.Where(r => r.Name.ToLower().Contains(search.ToLower()));

    if (type.HasValue)
      Query.Where(r => r.Type == type.Value);

    if (category.HasValue)
      Query.Where(r => r.Category == category.Value);

    Query.OrderBy(r => r.Category).ThenBy(r => r.Type).ThenBy(r => r.Name);
  }
}
