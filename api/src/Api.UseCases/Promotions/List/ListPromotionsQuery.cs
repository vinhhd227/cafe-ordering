using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.List;

public record ListPromotionsQuery(
  bool? IsActive = null,
  string? Scope = null,
  string? DiscountType = null,
  int Page = 1,
  int PageSize = 20) : IQuery<Result<PagedPromotionsDto>>;

public record PagedPromotionsDto(
  List<PromotionDto> Items,
  int TotalCount,
  int Page,
  int PageSize);
