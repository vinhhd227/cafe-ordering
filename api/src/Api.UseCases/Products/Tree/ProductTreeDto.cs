namespace Api.UseCases.Products.Tree;

public record ProductTreeItemDto(int Id, string Name);

public record ProductTreeCategoryDto(int Id, string Name, List<ProductTreeItemDto> Products);
