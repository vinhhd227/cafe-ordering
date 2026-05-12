namespace Api.UseCases.ProductOptionGroups.Delete;

public record DeleteProductOptionGroupCommand(int Id, string DeletedBy) : ICommand<Result>;
