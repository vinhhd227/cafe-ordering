using Api.Core.Aggregates.ProductOptionGroupAggregate;

namespace Api.UseCases.ProductOptionGroups.ToggleActive;

public class ToggleProductOptionGroupHandler(IRepositoryBase<ProductOptionGroup> repository)
  : ICommandHandler<ToggleProductOptionGroupCommand, Result>
{
  public async ValueTask<Result> Handle(ToggleProductOptionGroupCommand request, CancellationToken ct)
  {
    var group = await repository.GetByIdAsync(request.Id, ct);

    if (group is null)
      return Result.NotFound($"ProductOptionGroup {request.Id} not found.");

    group.ToggleActive();
    await repository.UpdateAsync(group, ct);

    return Result.Success();
  }
}
