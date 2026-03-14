using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.ListPublic;

public record ListPublicPromotionsQuery : IQuery<Result<List<PromotionDto>>>;
