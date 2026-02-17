namespace Api.UseCases.Categories.Update;

/// <summary>
///   Command cập nhật tên Category
/// </summary>
public record UpdateCategoryCommand(int CategoryId, string Name) : Common.Interfaces.ICommand<Result>;
