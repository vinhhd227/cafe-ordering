using Api.Core.Aggregates.ProductOptionGroupAggregate;
using Api.Core.Aggregates.ProductOptionGroupAggregate.Specifications;

namespace Api.UseCases.ProductOptionGroups.Delete;

public class DeleteProductOptionGroupHandler(IRepositoryBase<ProductOptionGroup> repository)
  : ICommandHandler<DeleteProductOptionGroupCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteProductOptionGroupCommand request, CancellationToken ct)
  {
    var group = await repository.GetByIdAsync(request.Id, ct);

    if (group is null)
      return Result.NotFound($"ProductOptionGroup {request.Id} not found.");

    group.Delete(request.DeletedBy);
    await repository.UpdateAsync(group, ct);

    return Result.Success();
  }
}
