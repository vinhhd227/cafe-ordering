using Api.Core.Aggregates.ProductOptionGroupAggregate;
using Api.Core.Aggregates.ProductOptionGroupAggregate.Specifications;

namespace Api.UseCases.ProductOptionGroups.ToggleValueStock;

public class ToggleValueStockHandler(IRepositoryBase<ProductOptionGroup> repository)
  : ICommandHandler<ToggleValueStockCommand, Result>
{
  public async ValueTask<Result> Handle(ToggleValueStockCommand request, CancellationToken ct)
  {
    var spec = new ProductOptionGroupByIdWithValuesSpec(request.GroupId);
    var group = await repository.FirstOrDefaultAsync(spec, ct);

    if (group is null)
      return Result.NotFound($"ProductOptionGroup {request.GroupId} not found.");

    var found = group.ToggleValueStock(request.ValueId);
    if (!found)
      return Result.NotFound($"Value {request.ValueId} not found in group {request.GroupId}.");

    await repository.UpdateAsync(group, ct);
    return Result.Success();
  }
}
