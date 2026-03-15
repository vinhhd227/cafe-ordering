namespace Api.UseCases.Promotions.Validate;

public record ValidatePromotionQuery(string Code, decimal? OrderAmount) : IQuery<Result<ValidatePromotionResult>>;

public record ValidatePromotionResult(
  int PromotionId,
  string Name,
  string Code,
  string CodeVisibility,
  string DiscountType,
  decimal DiscountValue,
  decimal? MaxDiscountAmount,
  int? BuyQuantity,
  int? GetQuantity,
  string Scope,
  List<int> ApplicableProductIds,
  List<int> ApplicableCategoryIds,
  List<int>? GetFromProductIds,
  List<int>? GetFromCategoryIds,
  decimal? MinOrderAmount,
  DateTime StartDate,
  DateTime? EndDate,
  decimal? EstimatedDiscount,
  bool IsApplicable,
  string? Message
);
