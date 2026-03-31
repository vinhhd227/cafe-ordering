namespace Api.UseCases.Products.Tree;

public record ProductTreeItemDto(int Id, string Name, decimal Price);

public record ProductTreeCategoryDto(int Id, string Name, List<ProductTreeItemDto> Products);
