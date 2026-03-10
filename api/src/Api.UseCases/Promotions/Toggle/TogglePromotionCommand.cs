using Api.UseCases.Promotions.DTOs;

namespace Api.UseCases.Promotions.Toggle;

public record TogglePromotionCommand(int Id, bool Activate) : ICommand<Result<PromotionDto>>;
