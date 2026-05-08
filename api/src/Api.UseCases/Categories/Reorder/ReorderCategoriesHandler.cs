using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.CategoryAggregate.Specifications;

namespace Api.UseCases.Categories.Reorder;

/// <summary>
///   Handler cập nhật SortOrder cho từng Category theo thứ tự IDs truyền vào.
/// </summary>
public class ReorderCategoriesHandler(IRepositoryBase<Category> repository)
  : Common.Interfaces.ICommandHandler<ReorderCategoriesCommand, Result>
{
  public async ValueTask<Result> Handle(ReorderCategoriesCommand request, CancellationToken ct)
  {
    if (request.Ids.Count == 0)
      return Result.Success();

    var spec = new CategoriesByIdsSpec(request.Ids);
    var categories = await repository.ListAsync(spec, ct);

    foreach (var (id, index) in request.Ids.Select((id, i) => (id, i)))
    {
      var cat = categories.FirstOrDefault(c => c.Id == id);
      cat?.SetSortOrder(index + 1);
    }

    await repository.UpdateRangeAsync(categories, ct);

    return Result.Success();
  }
}
