using Api.Core.Aggregates.ProductAggregate;

namespace Api.UseCases.Products.Update;

/// <summary>
///   Handler cập nhật Product details
/// </summary>
public class UpdateProductHandler : ICommandHandler<UpdateProductCommand, Result>
{
  private readonly IRepositoryBase<Product> _repository;

  public UpdateProductHandler(IRepositoryBase<Product> repository)
  {
    _repository = repository;
  }

  public async ValueTask<Result> Handle(UpdateProductCommand request, CancellationToken ct)
  {
    var product = await _repository.GetByIdAsync(request.ProductId, ct);

    if (product is null)
    {
      return Result.NotFound($"Product {request.ProductId} not found");
    }

    product.UpdateDetails(
      request.Name,
      request.Price,
      request.Description,
      request.ImageUrl);

    await _repository.UpdateAsync(product, ct);

    return Result.Success();
  }
}
