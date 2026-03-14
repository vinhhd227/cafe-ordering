namespace Api.Core.Aggregates.PromotionAggregate.Specifications;

/// <summary>
///   Lấy tất cả khuyến mãi PUBLIC đang hoạt động (cho client "Tìm voucher").
///   Lọc: active, chưa bị xóa, trong thời hạn, còn lượt dùng.
/// </summary>
public class PublicActivePromotionsSpec : Specification<Promotion>
{
  public PublicActivePromotionsSpec(DateTime utcNow) =>
    Query
      .Where(p => !p.IsDeleted
               && p.IsActive
               && p.CodeVisibility == CodeVisibility.Public
               && p.StartDate <= utcNow
               && (p.EndDate == null || p.EndDate >= utcNow)
               && (p.MaxUsage == null || p.CurrentUsage < p.MaxUsage))
      .OrderBy(p => p.Name);
}
