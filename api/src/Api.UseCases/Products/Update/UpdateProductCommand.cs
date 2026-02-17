namespace Api.UseCases.Products.Update;

/// <summary>
///   Command cập nhật Product details
/// </summary>
public record UpdateProductCommand(
  int ProductId,
  string Name,
  decimal Price,
  string? Description = null,
  string? ImageUrl = null
) : ICommand<Result>;
