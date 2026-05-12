using Api.UseCases.ProductOptionGroups.DTOs;

namespace Api.UseCases.ProductOptionGroups.List;

public record ListProductOptionGroupsQuery(
  string? SearchTerm = null,
  bool? IsActive = null) : IQuery<Result<List<ProductOptionGroupSummaryDto>>>;
