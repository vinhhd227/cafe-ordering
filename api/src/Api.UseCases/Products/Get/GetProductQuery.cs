using Api.UseCases.Products.DTOs;

namespace Api.UseCases.Products.Get;

/// <summary>
///   Query lấy chi tiết Product theo Id
/// </summary>
public record GetProductQuery(int ProductId) : IQuery<Result<ProductDto>>;
