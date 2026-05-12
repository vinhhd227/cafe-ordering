namespace Api.UseCases.ProductOptionGroups.ToggleActive;

public record ToggleProductOptionGroupCommand(int Id) : ICommand<Result>;
