using Api.Core.Aggregates.ProductAggregate;

namespace Api.UseCases.Products.Delete;

/// <summary>
///   Handler soft delete Product.
///   Gọi entity.Delete() thay vì repository.DeleteAsync()
///   để trigger domain event và set IsDeleted flag.
/// </summary>
public class DeleteProductHandler(IRepositoryBase<Product> repository) : ICommandHandler<DeleteProductCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteProductCommand request, CancellationToken ct)
  {
    var product = await repository.GetByIdAsync(request.ProductId, ct);

    if (product is null)
    {
      return Result.NotFound($"Product {request.ProductId} not found");
    }

    if (product.IsDeleted)
    {
      return Result.Error("Product đã bị xóa trước đó");
    }

    // Soft delete - gọi method trên entity
    product.Delete(request.DeletedBy);

    await repository.UpdateAsync(product, ct);

    return Result.Success();
  }
}
