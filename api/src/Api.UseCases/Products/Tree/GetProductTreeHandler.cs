using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;

namespace Api.UseCases.Products.Tree;

public class GetProductTreeHandler(IReadRepositoryBase<Product> repository)
  : IQueryHandler<GetProductTreeQuery, Result<List<ProductTreeCategoryDto>>>
{
  public async ValueTask<Result<List<ProductTreeCategoryDto>>> Handle(
    GetProductTreeQuery query,
    CancellationToken ct)
  {
    var products = await repository.ListAsync(new ActiveProductsWithCategorySpec(), ct);

    var tree = products
      .GroupBy(p => new { p.CategoryId, CategoryName = p.Category!.Name })
      .OrderBy(g => g.Key.CategoryName)
      .Select(g => new ProductTreeCategoryDto(
        g.Key.CategoryId,
        g.Key.CategoryName,
        g.Select(p => new ProductTreeItemDto(p.Id, p.Name, p.Price))
          .OrderBy(p => p.Name)
          .ToList()))
      .ToList();

    return Result.Success(tree);
  }
}
