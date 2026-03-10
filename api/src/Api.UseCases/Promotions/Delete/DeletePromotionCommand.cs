namespace Api.UseCases.Promotions.Delete;

public record DeletePromotionCommand(int Id, string DeletedBy) : ICommand<Result>;
