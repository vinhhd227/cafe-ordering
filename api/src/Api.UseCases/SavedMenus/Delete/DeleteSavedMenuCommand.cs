namespace Api.UseCases.SavedMenus.Delete;

public record DeleteSavedMenuCommand(int Id, string DeletedBy) : ICommand<Result>;
