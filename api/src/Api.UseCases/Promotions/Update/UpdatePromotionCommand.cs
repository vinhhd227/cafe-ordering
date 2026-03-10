using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.Update;

public record UpdatePromotionCommand(
  int Id,
  string Name,
  string? Code,
  string? Description,
  string DiscountType,
  decimal DiscountValue,
  int? BuyQuantity,
  int? GetQuantity,
  string Scope,
  List<int>? ApplicableProductIds,
  List<int>? ApplicableCategoryIds,
  string StackPolicy,
  decimal? MinOrderAmount,
  DateTime StartDate,
  DateTime? EndDate,
  int? MaxUsage) : ICommand<Result<PromotionDto>>;
