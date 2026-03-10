namespace Api.UseCases.Promotions.Validate;

public record ValidatePromotionQuery(string Code, decimal? OrderAmount) : IQuery<Result<ValidatePromotionResult>>;

public record ValidatePromotionResult(
  int PromotionId,
  string Name,
  string? Code,
  string DiscountType,
  decimal DiscountValue,
  string Scope,
  string StackPolicy,
  decimal? MinOrderAmount,
  DateTime StartDate,
  DateTime? EndDate,
  decimal? EstimatedDiscount,
  bool IsApplicable,
  string? Message
);
