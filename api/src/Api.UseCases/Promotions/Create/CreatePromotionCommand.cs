using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.Create;

public record CreatePromotionCommand(
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
