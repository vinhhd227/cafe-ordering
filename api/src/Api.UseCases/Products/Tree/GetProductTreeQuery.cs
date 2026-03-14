namespace Api.UseCases.Products.Tree;

public record GetProductTreeQuery : IQuery<Result<List<ProductTreeCategoryDto>>>;
