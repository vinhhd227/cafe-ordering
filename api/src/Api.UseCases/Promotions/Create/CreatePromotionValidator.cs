using FluentValidation;

namespace Api.UseCases.Promotions.Create;

public class CreatePromotionValidator : AbstractValidator<CreatePromotionCommand>
{
  private static readonly string[] ValidDiscountTypes = ["PERCENTAGE", "FIXED", "BUY_X_GET_Y"];
  private static readonly string[] ValidScopes        = ["ORDER", "PRODUCT", "CATEGORY"];
  private static readonly string[] ValidStackPolicies = ["EXCLUSIVE", "STACKABLE"];

  public CreatePromotionValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty().MaximumLength(200);

    RuleFor(x => x.Code)
      .MaximumLength(50)
      .When(x => x.Code is not null);

    RuleFor(x => x.Description)
      .MaximumLength(500)
      .When(x => x.Description is not null);

    RuleFor(x => x.DiscountType)
      .NotEmpty()
      .Must(v => ValidDiscountTypes.Contains(v?.ToUpperInvariant()))
      .WithMessage($"DiscountType must be one of: {string.Join(", ", ValidDiscountTypes)}");

    RuleFor(x => x.DiscountValue)
      .GreaterThanOrEqualTo(0)
      .When(x => x.DiscountType?.ToUpperInvariant() != "BUY_X_GET_Y");

    RuleFor(x => x.DiscountValue)
      .LessThanOrEqualTo(100)
      .When(x => x.DiscountType?.ToUpperInvariant() == "PERCENTAGE")
      .WithMessage("Percentage discount must be between 0 and 100.");

    RuleFor(x => x.BuyQuantity)
      .NotNull().GreaterThan(0)
      .When(x => x.DiscountType?.ToUpperInvariant() == "BUY_X_GET_Y")
      .WithMessage("BuyQuantity is required for BUY_X_GET_Y discount type.");

    RuleFor(x => x.GetQuantity)
      .NotNull().GreaterThan(0)
      .When(x => x.DiscountType?.ToUpperInvariant() == "BUY_X_GET_Y")
      .WithMessage("GetQuantity is required for BUY_X_GET_Y discount type.");

    RuleFor(x => x.Scope)
      .NotEmpty()
      .Must(v => ValidScopes.Contains(v?.ToUpperInvariant()))
      .WithMessage($"Scope must be one of: {string.Join(", ", ValidScopes)}");

    RuleFor(x => x.ApplicableProductIds)
      .NotEmpty()
      .When(x => x.Scope?.ToUpperInvariant() == "PRODUCT")
      .WithMessage("ApplicableProductIds required when Scope is PRODUCT.");

    RuleFor(x => x.ApplicableCategoryIds)
      .NotEmpty()
      .When(x => x.Scope?.ToUpperInvariant() == "CATEGORY")
      .WithMessage("ApplicableCategoryIds required when Scope is CATEGORY.");

    RuleFor(x => x.StackPolicy)
      .NotEmpty()
      .Must(v => ValidStackPolicies.Contains(v?.ToUpperInvariant()))
      .WithMessage($"StackPolicy must be one of: {string.Join(", ", ValidStackPolicies)}");

    RuleFor(x => x.MinOrderAmount)
      .GreaterThanOrEqualTo(0)
      .When(x => x.MinOrderAmount.HasValue);

    RuleFor(x => x.StartDate)
      .NotEmpty();

    RuleFor(x => x.EndDate)
      .GreaterThan(x => x.StartDate)
      .When(x => x.EndDate.HasValue)
      .WithMessage("EndDate must be after StartDate.");

    RuleFor(x => x.MaxUsage)
      .GreaterThan(0)
      .When(x => x.MaxUsage.HasValue);
  }
}
