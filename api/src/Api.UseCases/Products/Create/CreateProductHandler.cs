using Api.Core.Aggregates.ProductAggregate;

namespace Api.UseCases.Products.Create;

/// <summary>
///   Handler tạo mới Product
/// </summary>
public class CreateProductHandler(IRepositoryBase<Product> repository)
  : ICommandHandler<CreateProductCommand, Result<int>>
{
  public async ValueTask<Result<int>> Handle(CreateProductCommand request, CancellationToken ct)
  {
    var product = Product.Create(
      request.CategoryId,
      request.Name,
      request.Price,
      request.Description,
      request.ImageUrl,
      request.HasTemperatureOption,
      request.HasIceLevelOption,
      request.HasSugarLevelOption);

    await repository.AddAsync(product, ct);

    return Result.Success(product.Id);
  }
}
