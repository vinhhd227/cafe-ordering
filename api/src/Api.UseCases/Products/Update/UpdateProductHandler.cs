using Api.Core.Aggregates.ProductAggregate;

namespace Api.UseCases.Products.Update;

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
      return Result.NotFound($"Product {request.ProductId} not found");

    product.ChangeCategory(request.CategoryId);

    product.UpdateDetails(
      request.Name,
      request.Price,
      request.Description,
      request.ImageUrl);

    product.UpdateAccompaniment(request.IsAccompaniment);
    product.SetEstimatedPrepTime(request.EstimatedPrepMinutes);
    product.SetCostPrice(request.CostPrice);
    product.SetDiscountPrice(request.DiscountPrice);
    product.SetSku(request.Sku);
    product.SetBarcode(request.Barcode);

    if (request.IsActive)
      product.Activate();
    else
      product.Deactivate();

    await _repository.UpdateAsync(product, ct);

    return Result.Success();
  }
}
