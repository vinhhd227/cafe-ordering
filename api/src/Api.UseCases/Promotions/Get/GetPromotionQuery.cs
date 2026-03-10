using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.Get;

public record GetPromotionQuery(int? Id, string? Code) : IQuery<Result<PromotionDto>>;
