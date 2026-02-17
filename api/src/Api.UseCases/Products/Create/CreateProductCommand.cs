namespace Api.UseCases.Products.Create;

/// <summary>
///   Command tạo mới Product
/// </summary>
public record CreateProductCommand(
  int CategoryId,
  string Name,
  decimal Price,
  string? Description = null,
  string? ImageUrl = null,
  bool HasTemperatureOption = false,
  bool HasIceLevelOption = false,
  bool HasSugarLevelOption = false
) : ICommand<Result<int>>;
