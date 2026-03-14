using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.Update;

public record UpdatePromotionCommand(
  int Id,
  string Name,
  string? Code,
  string CodeVisibility,
  string? Description,
  string DiscountType,
  decimal DiscountValue,
  decimal? MaxDiscountAmount,
  int? BuyQuantity,
  int? GetQuantity,
  string Scope,
  List<int>? ApplicableProductIds,
  List<int>? ApplicableCategoryIds,
  List<int>? GetFromProductIds,
  List<int>? GetFromCategoryIds,
  decimal? MinOrderAmount,
  DateTime StartDate,
  DateTime? EndDate,
  int? MaxUsage) : ICommand<Result<PromotionDto>>;
