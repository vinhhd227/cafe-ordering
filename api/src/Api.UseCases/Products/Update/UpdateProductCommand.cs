namespace Api.UseCases.Products.Update;

/// <summary>
///   Command cập nhật Product details
/// </summary>
public record UpdateProductCommand(
  int ProductId,
  int CategoryId,
  string Name,
  decimal Price,
  bool IsActive,
  bool HasTemperatureOption,
  bool HasIceLevelOption,
  bool HasSugarLevelOption,
  string? Description = null,
  string? ImageUrl = null
) : ICommand<Result>;
