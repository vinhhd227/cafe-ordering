namespace Api.UseCases.Categories.Reorder;

/// <summary>
///   Command cập nhật thứ tự hiển thị của Categories.
///   Ids là danh sách ID theo thứ tự mới (index 0 = đầu tiên).
/// </summary>
public record ReorderCategoriesCommand(List<int> Ids) : Common.Interfaces.ICommand<Result>;
