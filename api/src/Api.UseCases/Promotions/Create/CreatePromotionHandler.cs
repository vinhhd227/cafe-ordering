using Api.Core.Aggregates.PromotionAggregate;
using Api.Core.Aggregates.PromotionAggregate.Specifications;
using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.Create;

public class CreatePromotionHandler(IRepositoryBase<Promotion> repo)
  : ICommandHandler<CreatePromotionCommand, Result<PromotionDto>>
{
  public async ValueTask<Result<PromotionDto>> Handle(CreatePromotionCommand cmd, CancellationToken ct)
  {
    // Resolve or auto-generate code
    var code = !string.IsNullOrWhiteSpace(cmd.Code)
      ? cmd.Code.Trim().ToUpperInvariant()
      : GenerateCode();

    // Unique code check
    var existing = await repo.FirstOrDefaultAsync(new PromotionByCodeSpec(code), ct);
    if (existing is not null)
      return Result.Invalid(new ValidationError("Code", "A promotion with this code already exists."));

    DiscountType   discountType    = DiscountType.FromName(cmd.DiscountType, true);
    PromotionScope scope           = PromotionScope.FromName(cmd.Scope, true);
    CodeVisibility codeVisibility  = CodeVisibility.FromName(cmd.CodeVisibility, true);

    var promo = Promotion.Create(
      cmd.Name, code, codeVisibility, cmd.Description,
      discountType, cmd.DiscountValue, cmd.MaxDiscountAmount,
      cmd.BuyQuantity, cmd.GetQuantity,
      scope, cmd.ApplicableProductIds, cmd.ApplicableCategoryIds,
      cmd.GetFromProductIds, cmd.GetFromCategoryIds,
      cmd.MinOrderAmount,
      DateTime.SpecifyKind(cmd.StartDate, DateTimeKind.Utc),
      cmd.EndDate.HasValue ? DateTime.SpecifyKind(cmd.EndDate.Value, DateTimeKind.Utc) : null,
      cmd.MaxUsage);

    await repo.AddAsync(promo, ct);

    return Result.Success(ToDto(promo));
  }

  private static string GenerateCode()
  {
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    var suffix = new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    return $"CAFE-{suffix}";
  }

  internal static PromotionDto ToDto(Promotion p) => new(
    p.Id, p.Name, p.Code,
    p.CodeVisibility.Name.ToUpperInvariant(),
    p.Description,
    p.DiscountType.Name.ToUpperInvariant(),
    p.DiscountValue, p.MaxDiscountAmount,
    p.BuyQuantity, p.GetQuantity,
    p.Scope.Name.ToUpperInvariant(),
    p.ApplicableProductIds, p.ApplicableCategoryIds,
    p.GetFromProductIds, p.GetFromCategoryIds,
    p.MinOrderAmount, p.StartDate, p.EndDate,
    p.MaxUsage, p.CurrentUsage, p.IsActive,
    p.CreatedAt, p.UpdatedAt);
}
