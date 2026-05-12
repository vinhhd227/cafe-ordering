namespace Api.UseCases.ProductOptionGroups.UnlinkFromProduct;

public record UnlinkGroupFromProductCommand(int GroupId, int ProductId) : ICommand<Result>;
