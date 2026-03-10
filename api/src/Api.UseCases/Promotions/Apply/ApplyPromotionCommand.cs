using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Promotions.Apply;

/// <summary>
///   Áp dụng một promotion vào order theo code hoặc PromotionId.
///   SessionId dùng để kiểm tra ownership (chỉ cần ở client-facing endpoint).
/// </summary>
public record ApplyPromotionCommand(
  int OrderId,
  string? Code,
  int? PromotionId,
  Guid? SessionId) : ICommand<Result<OrderDto>>;
