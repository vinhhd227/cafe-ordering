using Api.Core.Aggregates.ProductOptionGroupAggregate;
using Api.Core.Aggregates.ProductOptionGroupAggregate.Specifications;

namespace Api.UseCases.ProductOptionGroups.Update;

public class UpdateProductOptionGroupHandler(IRepositoryBase<ProductOptionGroup> repository)
  : ICommandHandler<UpdateProductOptionGroupCommand, Result>
{
  public async ValueTask<Result> Handle(UpdateProductOptionGroupCommand request, CancellationToken ct)
  {
    var spec = new ProductOptionGroupByIdWithValuesSpec(request.Id);
    var group = await repository.FirstOrDefaultAsync(spec, ct);

    if (group is null)
      return Result.NotFound($"ProductOptionGroup {request.Id} not found.");

    group.Update(request.Name, request.IsRequired, request.AllowMultiple, request.AllowQuantity);

    var valueData = request.Values
      .Select(v => new ProductOptionValueData(v.Name, v.Price, v.CostPrice))
      .ToList();
    group.ReplaceValues(valueData);

    await repository.UpdateAsync(group, ct);

    return Result.Success();
  }
}
