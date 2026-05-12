namespace Api.UseCases.ProductOptionGroups.LinkToProducts;

public record LinkGroupToProductsCommand(int GroupId, List<int> ProductIds) : ICommand<Result>;
