namespace Api.UseCases.Promotions.DTOs;

public record PromotionDto(
  int Id,
  string Name,
  string? Code,
  string? Description,
  string DiscountType,
  decimal DiscountValue,
  int? BuyQuantity,
  int? GetQuantity,
  string Scope,
  List<int> ApplicableProductIds,
  List<int> ApplicableCategoryIds,
  string StackPolicy,
  decimal? MinOrderAmount,
  DateTime StartDate,
  DateTime? EndDate,
  int? MaxUsage,
  int CurrentUsage,
  bool IsActive,
  DateTime CreatedAt,
  DateTime? UpdatedAt);
