using Api.UseCases.ProductOptionGroups.DTOs;

namespace Api.UseCases.ProductOptionGroups.Get;

public record GetProductOptionGroupQuery(int Id) : IQuery<Result<ProductOptionGroupDto>>;
