namespace Api.UseCases.Products.Delete;

/// <summary>
///   Command soft delete Product
/// </summary>
public record DeleteProductCommand(
  int ProductId,
  string DeletedBy
) : ICommand<Result>;
